﻿using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace Spreads.Serialization {

    internal static class ArrayConvertorFactory {
        public static IBinaryConverter<TElement[]> GenericCreate<TElement>() {
            return new ArrayBinaryConverter<TElement>();
        }
        public static object Create(Type type) {
            MethodInfo method = typeof(ArrayConvertorFactory).GetMethod("GenericCreate");
            MethodInfo generic = method.MakeGenericMethod(type);
            return generic.Invoke(null, null);
        }
    }

    internal class ArrayBinaryConverter<TElement> : IBinaryConverter<TElement[]> {
        public bool IsFixedSize => false;
        public int Size => 0;
        public int Version => TypeHelper<TElement>.Version;

        private static int _itemSize = TypeHelper<TElement>.Size;

        public int SizeOf(TElement[] value, ref MemoryStream memoryStream) {
            if (_itemSize > 0) {
                memoryStream = null;
                return _itemSize * value.Length;
            }
            throw new NotImplementedException();
        }

        public void ToPtr(TElement[] value, IntPtr ptr, MemoryStream memoryStream = null) {
            throw new NotImplementedException();
        }

        public TElement[] FromPtr(IntPtr ptr) {
            throw new NotImplementedException();
        }
    }


    internal class ByteArrayBinaryConverter : IBinaryConverter<byte[]> {
        public bool IsFixedSize => false;
        public int Size => 0;
        public int SizeOf(byte[] value, ref MemoryStream memoryStream) {
            return value.Length;
        }

        public void ToPtr(byte[] value, IntPtr ptr, MemoryStream memoryStream = null) {
            // version
            Marshal.WriteInt32(ptr, Version);
            // size
            Marshal.WriteInt32(ptr + 4, value.Length);
            // payload
            Marshal.Copy(value, 0, ptr + 8, value.Length);
        }

        public byte[] FromPtr(IntPtr ptr) {
            var version = Marshal.ReadInt32(ptr);
            if (version != 0) throw new NotSupportedException();
            var length = Marshal.ReadInt32(ptr + 4);
            var bytes = new byte[length];
            Marshal.Copy(ptr + 8, bytes, 0, length);
            return bytes;
        }

        public int Version => 0;
    }


    internal class StringBinaryConverter : IBinaryConverter<string> {
        public bool IsFixedSize => false;
        public int Size => 0;
        public int SizeOf(string value, ref MemoryStream memoryStream) {
            var maxLength = value.Length * 2;
            bool needReturn = false;
            byte[] buffer;
            if (maxLength < 8 * 1024) {
                buffer = BinaryConvertorExtensions.ThreadStaticBuffer;
            } else {
                needReturn = true;
                buffer = OptimizationSettings.ArrayPool.Take<byte>(maxLength);
            }
            var len = Encoding.UTF8.GetBytes(value, 0, value.Length, buffer, 0);
            if (memoryStream == null) {
                memoryStream = new MemoryStream();
            }
            var initPosition = memoryStream.Position;
            memoryStream.Position = initPosition + 8;
            memoryStream.Write(buffer, 0, len);
            var finalPosition = memoryStream.Position;
            memoryStream.Position = initPosition;
            memoryStream.WriteAsPtr<int>(Version);
            memoryStream.WriteAsPtr<int>(len);
            memoryStream.Position = finalPosition;

            if (needReturn) OptimizationSettings.ArrayPool.Return(buffer);
            return len + 8;
        }

        public unsafe void ToPtr(string value, IntPtr ptr, MemoryStream memoryStream = null) {
            if (memoryStream == null) {
                // version
                Marshal.WriteInt32(ptr, Version);
                // payload
                var maxLength = value.Length * 2;
                fixed (char* charPtr = value)
                {
                    var len = Encoding.UTF8.GetBytes(charPtr, value.Length, (byte*)ptr + 8, maxLength);
                    // size
                    Marshal.WriteInt32(ptr + 4, len);
                }

            } else {
                memoryStream.WriteToPtr(ptr);
            }

        }

        public string FromPtr(IntPtr ptr) {
            var version = Marshal.ReadInt32(ptr);
            if (version != 0) throw new NotSupportedException();
            var length = Marshal.ReadInt32(ptr + 4);
            bool needReturn = false;
            byte[] buffer;
            if (length < BinaryConvertorExtensions.MaxBufferSize) {
                buffer = BinaryConvertorExtensions.ThreadStaticBuffer;
            } else {
                needReturn = true;
                buffer = OptimizationSettings.ArrayPool.Take<byte>(length);
            }
            Marshal.Copy(ptr + 8, buffer, 0, length);
            var value = Encoding.UTF8.GetString(buffer, 0, length);
            if (needReturn) OptimizationSettings.ArrayPool.Return(buffer);
            return value;
        }

        public int Version => 0;
    }


    internal class MemoryStreamBinaryConverter : IBinaryConverter<MemoryStream> {
        public bool IsFixedSize => false;
        public int Size => 0;
        public int SizeOf(MemoryStream value, ref MemoryStream memoryStream) {
            if (value.Length > int.MaxValue) throw new ArgumentOutOfRangeException("Memory stream is too large");
            return (int)value.Length;
        }


        public unsafe void ToPtr(MemoryStream value, IntPtr ptr, MemoryStream memoryStream = null) {
            if (value.Length > int.MaxValue) throw new ArgumentOutOfRangeException("Memory stream is too large");
            // version
            Marshal.WriteInt32(ptr, 0);
            // size
            Marshal.WriteInt32(ptr + 4, (int)value.Length);
            // payload
            var rms = value as Spreads.Serialization.Microsoft.IO.RecyclableMemoryStream;
            if (rms != null) {
                throw new NotImplementedException("TODO use RecyclableMemoryStream internally");
            }
            ptr = ptr + 8;
            int b;
            while ((b = value.ReadByte()) >= 0) {
                *(byte*)ptr = (byte)b;
            }
        }

        public MemoryStream FromPtr(IntPtr ptr) {
            var version = Marshal.ReadInt32(ptr);
            if (version != 0) throw new NotSupportedException();
            var length = Marshal.ReadInt32(ptr + 4);
            var bytes = new byte[length];
            Marshal.Copy(ptr + 8, bytes, 0, length);
            return new MemoryStream(bytes);
        }

        public int Version => 0;
    }
}
