﻿using System;
using CoreVideo;
using Foundation;
using ObjCRuntime;
using System.Collections.Generic;

namespace Shiny.BluetoothLE;


public class OutputStream : System.IO.Stream
{
    readonly List<byte> buffer = new();
    readonly NSOutputStream stream;

    public OutputStream(NSOutputStream stream) => this.stream = stream;


    public override void Flush()
    {
        this.Write(this.buffer.ToArray(), 0, this.buffer.Count);
        this.buffer.Clear();
    }


    public override int Read(byte[] buffer, int offset, int count)
        => throw new NotSupportedException();

    public override long Seek(long offset, System.IO.SeekOrigin origin)
        => throw new NotSupportedException();

    public override void SetLength(long value)
        => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count)
    {
        if (offset != 0)
            throw new NotSupportedException();

        var stillToSend = new byte[buffer.Length - offset];
        this.buffer.CopyTo(stillToSend, offset);

        while (count > 0)
        {
            if (this.stream.HasSpaceAvailable())
            {
                var bytesWritten = this.stream.Write(buffer, (uint)count);
                if (bytesWritten == -1)
                {
                    throw new InvalidOperationException("Write error: -1 returned");
                }
                else if (bytesWritten > 0)
                {
                    Console.WriteLine("Bytes written: " + bytesWritten.ToString());
                    count -= (int)bytesWritten;
                    if (0 == count)
                        break;

                    var temp = new List<byte>();
                    for (var i = bytesWritten; i < stillToSend.Length; i++)
                    {
                        temp.Add(stillToSend[i]);
                    }
                    stillToSend = temp.ToArray();
                }
            }
        }
    }

    public override void WriteByte(byte value)
        => this.buffer.Add(value);

    public override bool CanRead => false;
    public override bool CanSeek => false;
    public override bool CanWrite => true;
    public override long Length => throw new NotSupportedException();
    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }
}