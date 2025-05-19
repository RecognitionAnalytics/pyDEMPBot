using CommunityToolkit.Mvvm.Messaging;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Dempbot4.Models.ScriptEngines.Messages
{
    public class StreamWriteEvent : MemoryStream
    {

        public void Write(string message)
        {
            WeakReferenceMessenger.Default.Send<Console_MSG>(new Console_MSG { Command = message });
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            WeakReferenceMessenger.Default.Send<Console_MSG>(new Console_MSG { Command = Encoding.Default.GetString(buffer, offset, count) });

            base.Write(buffer, offset, count);
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            WeakReferenceMessenger.Default.Send<Console_MSG>(new Console_MSG { Command = Encoding.Default.GetString(buffer, offset, count) });


            return base.WriteAsync(buffer, offset, count, cancellationToken);
        }

        public override void WriteByte(byte value)
        {
            WeakReferenceMessenger.Default.Send<Console_MSG>(new Console_MSG { Command = value.ToString() });


            base.WriteByte(value);
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            WeakReferenceMessenger.Default.Send<Console_MSG>(new Console_MSG { Command = Encoding.Default.GetString(buffer, offset, count) });

            return base.BeginWrite(buffer, offset, count, callback, state);
        }
    }
}
