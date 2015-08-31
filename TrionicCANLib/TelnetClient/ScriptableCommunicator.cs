
# region Heading

/**************************************************************************************************************/
/*                                                                                                            */
/*  TelnetSocket (DotNET 3.5)                                                                                 */
/*  A wrapper for a System.Net.Sockets.TcpClient that performs simple Telnet negotiation and is scriptable    */
/*  www.codeproject.com/Articles/63201/TelnetSocket                                                           */
/*                                                                                                            */
/*  ScriptableCommunicator.cs                                                                                 */
/*                                                                                                            */
/*  Implements an interface and abstract class for the CommScript class to use                                */
/*                                                                                                            */
/*  This is free code, use it as you require. If you modify it please use your own namespace.                 */
/*                                                                                                            */
/*  If you like it or have suggestions for improvements please let me know at: PIEBALDconsult@aol.com         */
/*                                                                                                            */
/*  Modification history:                                                                                     */
/*  2008-10-23          Sir John E. Boucher     Pulled from TelnetClient                                      */
/*  2010-03-01          Sir John E. Boucher     Pulled from CommScript                                        */
/*                                                                                                            */
/**************************************************************************************************************/

# endregion

namespace PIEBALD.Types
{
    public delegate void DataReceived ( string Data ) ;

    public delegate void ExceptionCaught ( System.Exception Exception ) ;

    public interface IScriptableCommunicator : System.IDisposable
    {
        void Connect   ( string Host ) ;
        void WriteLine ( string Data , params object[] Parameters ) ;
        void Write     ( string Data , params object[] Parameters ) ;
        void Close     () ;
        bool Connected () ;

        System.TimeSpan ResponseTimeout { get ; set ; } 

        System.Text.Encoding Encoding { get ; set ; }
        
        string LineTerminator { get ; set ; }

        event DataReceived    OnDataReceived    ;
        event ExceptionCaught OnExceptionCaught ;
    }

    public abstract class ScriptableCommunicator : IScriptableCommunicator
    {
        private System.Text.Encoding encoding       ;
        private string               lineterminator ;

        protected ScriptableCommunicator
        (
            System.TimeSpan      ResponseTimeout
        ,
            System.Text.Encoding Encoding
        ,
            string               LineTerminator
        )
        {
            this.ResponseTimeout = ResponseTimeout ;
            this.Encoding        = Encoding ;
            this.LineTerminator  = LineTerminator ;

            this.Timer           = null ;
        
            return ;
        }
    
        public abstract void Connect ( string Host ) ;
        public abstract void Write   ( string Data , params object[] Parameters ) ;
        public abstract void Close   () ;
        public abstract bool Connected();

        public virtual void
        WriteLine
        (
            string          Format
        ,
            params object[] Parameters
        )
        {
            this.Write ( Format + this.LineTerminator , Parameters ) ;

            return ;
        }

        public virtual System.TimeSpan ResponseTimeout { get ; set ; }

        public virtual System.Text.Encoding Encoding 
        { 
            get 
            {
                return ( this.encoding ) ;
            }
            
            set 
            {
                if ( value == null )
                {
                    throw ( new System.InvalidOperationException 
                        ( "The value of Encoding must not be null" ) ) ;
                }
                
                this.encoding = value ;
            
                return ;
            }
        }

        public virtual string 
        LineTerminator 
        { 
            get 
            {
                return ( this.lineterminator ) ;
            }
            
            set 
            {
                if ( value == null )
                {
                    throw ( new System.InvalidOperationException 
                        ( "The value of LineTerminator must not be null" ) ) ;
                }
                
                this.lineterminator = value ;
                
                return ;
            }
        }

        protected virtual System.Timers.Timer Timer { get ; set ; } 
    
        public event DataReceived OnDataReceived ;

        protected virtual void
        RaiseDataReceived
        (
            string Data
        )
        {
            if ( this.Timer != null )
            {
                this.Timer.Stop() ;
            }
            
            if ( this.OnDataReceived != null )
            {
                this.OnDataReceived ( Data ) ;
            }
            
            if ( this.Timer != null )
            {
                this.Timer.Start() ;
            }
            
            return ;
        }

        public event ExceptionCaught OnExceptionCaught ;

        protected virtual void
        RaiseExceptionCaught
        (
            System.Exception Exception
        )
        {
            if ( OnExceptionCaught != null )
            {
                OnExceptionCaught ( Exception ) ;
            }
            
            return ;
        }

        public virtual void
        Dispose
        (
        )
        {
            this.Close() ;
            
            return ;
        }
    }
}
