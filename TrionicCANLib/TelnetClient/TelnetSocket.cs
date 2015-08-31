
# region Heading

/**************************************************************************************************************/
/*                                                                                                            */
/*  TelnetSocket (DotNET 3.5)                                                                                 */
/*  A wrapper for a System.Net.Sockets.TcpClient that performs simple Telnet negotiation and is scriptable    */
/*  www.codeproject.com/Articles/63201/TelnetSocket                                                           */
/*                                                                                                            */
/*  TelnetSocket.cs                                                                                           */
/*                                                                                                            */
/*  Implements a scriptable Telnet socket                                                                     */
/*                                                                                                            */
/*  Some of this implementation is based on the work of someone else, but I don't remember whom.              */
/*                                                                                                            */
/*  This is free code, use it as you require. If you modify it please use your own namespace.                 */
/*                                                                                                            */
/*  If you like it or have suggestions for improvements please let me know at: PIEBALDconsult@aol.com         */
/*                                                                                                            */
/*  Modification history:                                                                                     */
/*  2005-02-01          Sir John E. Boucher     Created                                                       */
/*  2008-10-14          Sir John E. Boucher     Reworked to use generic collections                           */
/*                                              Added comments and Exceptions and stuff as well               */
/*  2008-10-22          Sir John E. Boucher     Removed the script-execution code                             */
/*  2010-02-28          Sir John E. Boucher     Renamed from TelnetClient                                     */
/*  2010-03-03          Sir John E. Boucher     Moved negotiation to the Negotiator                           */
/*                                              Several other changes                                         */
/*                                                                                                            */
/**************************************************************************************************************/

# endregion

namespace PIEBALD.Types
{
    /**
    <summary>
        A wrapper for a System.Net.Sockets.TcpClient that performs simple Telnet negotiation and is scriptable
    </summary>
    */
    public sealed class TelnetSocket : PIEBALD.Types.ScriptableCommunicator
    {

# region Fields

        private System.Net.Sockets.TcpClient     socket = null  ;
        private System.Net.Sockets.NetworkStream stream = null  ;
        private System.Threading.Thread          reader = null  ;
        private bool                             abort  = false ;

# endregion 
        
# region Constructor

        /**
        <summary>
            Instantiate a TelnetSocket, but do not connect
        </summary>
        */
        public TelnetSocket
        (
        )
        :
        base
        (
            new System.TimeSpan ( 0 , 0 , 10 )       // (0,1,0) 1 minute
        ,
            System.Text.Encoding.ASCII
        ,
            "\r"
        )
        {
            return ;
        }

# endregion

# region Public Methods
        
        /**
        <summary>
            Attempt to connect to the specified host
        </summary>
        <remarks>
            The default port is 23
        </remarks>
        <param name="Host">
            The name or IP address of the host, may contain a colon (:) followed by the port number
        </param>
        <exception cref="System.ArgumentNullException">
            If the Host value is null
        </exception>
        <exception cref="System.ArgumentException">
            If the Host value appears to contain a port number, but it can't be parsed successfully
        </exception>
        */
        public override void
        Connect
        (
            string Host
        )
        {
            if ( Host == null )
            {
                throw ( new System.ArgumentNullException ( "Host" , "Host must not be null" ) ) ;
            }

            string[] parts = Host.Split ( new char[] { ':' } , 2 ) ;
            int      port  = 23 ;

            if ( ( parts.Length > 1 ) && !int.TryParse ( parts [ 1 ] , out port ) )
            {
                throw ( new System.ArgumentException ( "Unable to parse the port" , "Host" ) ) ;
            }
            
            this.DoConnect ( parts [ 0 ] , port ) ;
            
            return ;
        }

        /**
        <summary>
            Attempt to connect to the specified host at the specfied port
        </summary>
        <param name="Host">
            The name or IP address of the host
        </param>
        <param name="Port">
            The network port number to use
        </param>
        <exception cref="System.ArgumentNullException">
            If the Host value is null
        </exception>
        */
        public void
        Connect
        (
            string Host
        ,
            int    Port
        )
        {
            if ( Host == null )
            {
                throw ( new System.ArgumentNullException ( "Host" , "Host must not be null" ) ) ;
            }

            this.DoConnect ( Host , Port ) ;
            
            return ;
        }

/**************************************************************************************************************/

        /**
        <summary>
            Write some text to the socket
            See System.String.Format
        </summary>
        <param name="Format">
            A format string
        </param>
        <param name="Parameters">
            Any parameters to format into the string
        </param>
        <exception cref="System.InvalidOperationException">
            If the connection isn't open
        </exception>
        */
        public override void
        Write
        (
            string          Format
        ,
            params object[] Parameters
        )
        {
            if ( ( this.socket == null ) || !this.socket.Connected )
            {
                throw ( new System.InvalidOperationException ( "The socket appears to be closed" ) ) ;
            }
            
            try
            {
                if ( ( Parameters != null ) && ( Parameters.Length > 0 ) )
                {
                    Format = System.String.Format
                    (
                        Format
                    ,
                        Parameters 
                    ) ;
                }
                
                byte[] data = this.Encoding.GetBytes ( Format ) ;

                lock ( this.stream )
                {
                    this.stream.Write ( data , 0 , data.Length ) ;
                }
            }
            catch ( System.Exception err )
            {
                this.RaiseExceptionCaught ( err ) ;
            
                throw ;
            }
            
            return ;
        }
        
/**************************************************************************************************************/

        /**
        <summary>
            Stop reading from the socket, and disconnect from the host
        </summary>
        */
        public override void
        Close
        (
        )
        {
            this.Abort() ;

            if ( this.socket != null )
            {
                this.stream = null ;

                this.socket.Close() ;

                this.socket = null ;
            }
            
            return ;
        }

/**************************************************************************************************************/

        /**
        <summary>
            Indicate if the underlying TcpSocket is connected
        </summary>
        <returns>
            bool: True if TcpSocket is connected, otherwise False
        </returns>
        */
        public override bool Connected()
        {
            return ((this.socket != null) && this.socket.Connected);
        }

/**************************************************************************************************************/

# endregion

# region Private methods
        
        private void
        DoConnect
        (
            string Host
        ,
            int    Port
        )
        {
            if ( this.socket != null )
            {
                this.Close() ;
            }
        
            this.socket = new System.Net.Sockets.TcpClient ( Host , Port ) ;
            this.socket.NoDelay = true ;
            
            this.stream = this.socket.GetStream() ;
            this.stream.ReadTimeout = 100 ;     // 100

            this.reader = new System.Threading.Thread ( this.Reader ) ;
            this.reader.Priority = System.Threading.ThreadPriority.BelowNormal ;
            this.reader.IsBackground = true ;
            this.reader.Start() ;

            if ( this.ResponseTimeout.TotalMilliseconds > 0 )
            {
                this.Timer = new System.Timers.Timer 
                    ( this.ResponseTimeout.TotalMilliseconds ) ;

                this.Timer.Elapsed += delegate
                (
                    object                         sender
                ,
                    System.Timers.ElapsedEventArgs args
                )
                {
                    this.Abort() ;
                    
                    throw ( new System.TimeoutException 
                        ( "The ResponseTimeout has expired" ) ) ;
                } ;

                this.Timer.Start() ;
            }
            
            return ;
        }

        private void
        Abort
        (
        )
        {
            this.abort = true ;
            
            if ( this.Timer != null )
            {
                this.Timer.Stop() ;
            }

            if ( this.reader != null )
            {
                if ( !this.reader.Join ( 15000 ) )
                {
                    this.reader.Abort() ;
                
                    this.reader.Join ( 15000 ) ;
                }
                
                this.reader = null ;
            }
            
            return ;
        }

        private void
        Reader
        (
        )
        {
            using 
            (
                Negotiator neg
            =
                new Negotiator ( this.stream )
            )
            {
                byte[] buffer = new byte [ this.socket.ReceiveBufferSize ] ;

                while ( !this.abort )
                {
                    int bytes = 0 ;
                    
                    lock ( this.stream )
                    {
                        if ( this.socket.Available > 0 )
                        {
                            bytes = this.stream.Read ( buffer , 0 , buffer.Length ) ;
                        } 
                    }
                    
                    if ( bytes > 0 )
                    {
                        bytes = neg.Negotiate ( buffer , bytes ) ;
                        
                        if ( bytes > 0 )
                        {
                            this.RaiseDataReceived ( this.Encoding.GetString ( buffer , 0 , bytes ) ) ;
                        }
                    }
                    else
                    {
                        System.Threading.Thread.Sleep ( 100 ) ;      // 100
                    }
                }
            }
            
            return ;
        }

# endregion

# region Negotiator class

        private sealed class Negotiator : System.IDisposable
        {
            private System.Net.Sockets.NetworkStream stream ;

            public Negotiator
            (
                System.Net.Sockets.NetworkStream Stream
            )
            {
                this.stream = Stream ;
                
                return ;
            }
            
            public void
            Dispose
            (
            )
            {
                if ( this.stream != null )
                {
                    this.stream = null ;
                }
                
                return ;
            }
            
            public int
            Negotiate
            (
                byte[] Buffer
            ,
                int    Count
            )
            {
                int resplen = 0 ;
                int index   = 0 ;

                while ( index < Count )
                {
                    if ( Buffer [ index ] == TelnetByte.IAC )
                    {
                        try
                        {
                            switch ( Buffer [ index + 1 ] )
                            {
                                /* If two IACs are together they represent one data byte 255 */
                                case TelnetByte.IAC :
                                {
                                    Buffer [ resplen++ ] = Buffer [ index ] ;

                                    index += 2 ;
                                    
                                    break ;
                                }
                                
                                /* Ignore the Go-Ahead command */
                                case TelnetByte.GA :
                                {
                                    index += 2 ;
                                    
                                    break ;
                                }
                                
                                /* Respond WONT to all DOs and DONTs */
                                case TelnetByte.DO   :
                                case TelnetByte.DONT :
                                {
                                    Buffer [ index + 1 ] = TelnetByte.WONT ;
                                    
                                    lock ( this.stream )
                                    {
                                        this.stream.Write ( Buffer , index , 3 ) ;
                                    }
                                    
                                    index += 3 ;

                                    break ;
                                }

                                /* Respond DONT to all WONTs */
                                case TelnetByte.WONT :
                                {
                                    Buffer [ index + 1 ] = TelnetByte.DONT ;
                                    
                                    lock ( this.stream )
                                    {
                                        this.stream.Write ( Buffer , index , 3 ) ;
                                    }
                                    
                                    index += 3 ;

                                    break ;
                                }
                                
                                /* Respond DO to WILL ECHO and WILL SUPPRESS GO-AHEAD */
                                /* Respond DONT to all other WILLs                    */
                                case TelnetByte.WILL :
                                {
                                    byte action = TelnetByte.DONT ;
                                    
                                    if ( Buffer [ index + 2 ] == TelnetByte.ECHO )
                                    {
                                        action = TelnetByte.DO ;
                                    }
                                    else if ( Buffer [ index + 2 ] == TelnetByte.SUPP )
                                    {
                                        action = TelnetByte.DO ;
                                    }
                                    
                                    Buffer [ index + 1 ] = action ;
                                    
                                    lock ( this.stream )
                                    {
                                        this.stream.Write ( Buffer , index , 3 ) ;
                                    }
                                    
                                    index += 3 ;

                                    break ;
                                }
                            }
                        }
                        catch ( System.IndexOutOfRangeException )
                        {
                            /* If there aren't enough bytes to form a command, terminate the loop */
                            index = Count ;
                        } 
                    }
                    else
                    {
                        if ( Buffer [ index ] != 0 )
                        {
                            Buffer [ resplen++ ] = Buffer [ index ] ;
                        }
                        
                        index++ ;
                    }
                }

                return ( resplen ) ;
            }

# region TelnetByte struct

            /*\
            |*| See also: http://en.wikipedia.org/wiki/Telnetd
            |*|           http://www.iana.org/assignments/telnet-options
            |*|           http://www.faqs.org/rfcs/rfc857.html
            |*|
            |*| I tried an enumeration, but it required casting, and provided no benefit over this
            \*/
            private struct TelnetByte
            {
                /* TELNET commands */
                public const byte GA   = 249 ;
                public const byte WILL = 251 ;
                public const byte WONT = 252 ;
                public const byte DO   = 253 ;
                public const byte DONT = 254 ;
                public const byte IAC  = 255 ;

                /* TELNET options */
                public const byte ECHO =   1 ;
                public const byte SUPP =   3 ;
            }
    
# endregion

        }

# endregion

    }
}
