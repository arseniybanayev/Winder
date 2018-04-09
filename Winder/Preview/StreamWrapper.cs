using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace Winder.Preview
{
	/// <summary>
	/// Very simple wrapper class implementation for the IStream interface.
	/// <remarks>Does not support advanced operations like the Revert() operation, options specified in Commit(), basic stat values, etc.</remarks>
	/// </summary>
	internal sealed class StreamWrapper : IStream
	{
		#region Constants

		private const long PositionNotSet = -1;
		private const int StgEInvalidfunction = 32774;
		private const int Chunk = 4096;

		private const string MethodNotSupported = "Method not supported.";
		private const string UnknownError = "Unknown error.";

		#endregion

		#region Private Data

		private long _indexPosition = PositionNotSet;
		private readonly Stream _stream = null;

		#endregion

		#region Constructor

		/// <summary>
		/// Initializes a new instance of the StreamWrapper with the specified input stream.
		/// </summary>
		/// <param name="stream">The stream being wrapped.</param>
		internal StreamWrapper(Stream stream) {
			_indexPosition = PositionNotSet;
			_stream = stream;
		}

		#endregion

		#region IStream Members

		public void Clone(out IStream ppstm) {
			// Operation not supported
			ppstm = null;
			throw new ExternalException(MethodNotSupported, StgEInvalidfunction);
		}

		public void Commit(int grfCommitFlags) {
			_stream.Flush();
		}

		public void CopyTo(IStream pstm, long cb, IntPtr pcbRead, IntPtr pcbWritten) {
			var buffer = new byte[Chunk];
			long written = 0;
			var read = 0;

			if (cb != 0) {
				SetSizeToPosition();
				do {
					var count = Chunk;
					if (written + Chunk > cb) {
						count = (int)(cb - written);
					}

					if ((read = _stream.Read(buffer, 0, count)) == 0) {
						break;
					}

					pstm.Write(buffer, read, IntPtr.Zero);
					written += read;

				} while (written < cb);
			}

			if (pcbRead != IntPtr.Zero) {
				Marshal.WriteInt64(pcbRead, written);
			}

			if (pcbWritten != IntPtr.Zero) {
				Marshal.WriteInt64(pcbWritten, written);
			}
		}

		public void LockRegion(long libOffset, long cb, int dwLockType) {
			// Operation not supported
			throw new ExternalException(MethodNotSupported, StgEInvalidfunction);
		}

		public void Read(byte[] pv, int cb, IntPtr pcbRead) {
			var read = 0;
			if (cb != 0) {
				SetSizeToPosition();
				read = _stream.Read(pv, 0, cb);
			}

			if (pcbRead != IntPtr.Zero) {
				Marshal.WriteInt32(pcbRead, read);
			}
		}

		public void Revert() {
			throw new ExternalException(MethodNotSupported, StgEInvalidfunction);
		}

		public void Seek(long dlibMove, int dwOrigin, IntPtr plibNewPosition) {
			long newPosition = 0;
			var seekOrigin = SeekOrigin.Begin;

			try {
				// attempt to cast parameter
				seekOrigin = (SeekOrigin)dwOrigin;
			} catch {
				throw new ExternalException(UnknownError, StgEInvalidfunction);
			}

			if (_stream.CanWrite) {
				switch (seekOrigin) {
					case SeekOrigin.Begin:
						newPosition = dlibMove;
						break;

					case SeekOrigin.Current:
						newPosition = _indexPosition;
						if (newPosition == PositionNotSet) {
							newPosition = _stream.Position;
						}

						newPosition += dlibMove;
						break;

					case SeekOrigin.End:
						newPosition = _stream.Length + dlibMove;
						break;

					default:
						// should never happen
						throw new ExternalException(UnknownError, StgEInvalidfunction);
				}

				if (newPosition > _stream.Length) {
					_indexPosition = newPosition;
				} else {
					_stream.Position = newPosition;
					_indexPosition = PositionNotSet;
				}
			} else {
				try {
					newPosition = _stream.Seek(dlibMove, seekOrigin);
				} catch (ArgumentException) {
					throw new ExternalException(UnknownError, StgEInvalidfunction);
				}

				_indexPosition = PositionNotSet;
			}

			if (plibNewPosition != IntPtr.Zero) {
				Marshal.WriteInt64(plibNewPosition, newPosition);
			}
		}

		public void SetSize(long libNewSize) {
			_stream.SetLength(libNewSize);
		}

		public void Stat(out System.Runtime.InteropServices.ComTypes.STATSTG pstatstg, int grfStatFlag) {
			pstatstg = new System.Runtime.InteropServices.ComTypes.STATSTG();
			pstatstg.cbSize = _stream.Length;
		}

		public void UnlockRegion(long libOffset, long cb, int dwLockType) {
			// Operation not supported
			throw new ExternalException(MethodNotSupported, StgEInvalidfunction);
		}

		public void Write(byte[] pv, int cb, IntPtr pcbWritten) {
			if (cb != 0) {
				SetSizeToPosition();
				_stream.Write(pv, 0, cb);
			}

			if (pcbWritten != IntPtr.Zero) {
				Marshal.WriteInt32(pcbWritten, cb);
			}
		}

		#endregion

		#region Private Methods

		private void SetSizeToPosition() {
			if (_indexPosition != PositionNotSet) {
				// position requested greater than current length?
				if (_indexPosition > _stream.Length) {
					// expand stream
					_stream.SetLength(_indexPosition);
				}

				// set new position
				_stream.Position = _indexPosition;
				_indexPosition = PositionNotSet;
			}
		}

		#endregion
	}
}