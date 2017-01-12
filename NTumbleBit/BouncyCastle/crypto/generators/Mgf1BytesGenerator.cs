﻿using System;
//using Org.BouncyCastle.Math;
//using Org.BouncyCastle.Security;
using NTumbleBit.BouncyCastle.Crypto;
using NTumbleBit.BouncyCastle.Crypto.Parameters;

namespace NTumbleBit.BouncyCastle.Crypto.Generators
{
	/**
    * Generator for MGF1 as defined in Pkcs 1v2
    */
	class Mgf1BytesGenerator
	{
		private IDigest digest;
		private byte[] seed;
		private int hLen;

		/**
        * @param digest the digest to be used as the source of Generated bytes
        */
		public Mgf1BytesGenerator(
			IDigest digest)
		{
			this.digest = digest;
			hLen = digest.GetDigestSize();
		}

		public void Init(MgfParameters parameters)
		{
			MgfParameters p = parameters;
			seed = p.GetSeed();
		}

		/**
        * return the underlying digest.
        */
		public IDigest Digest
		{
			get
			{
				return digest;
			}
		}

		/**
        * int to octet string.
        */
		private void ItoOSP(
			int i,
			byte[] sp)
		{
			sp[0] = (byte)((uint)i >> 24);
			sp[1] = (byte)((uint)i >> 16);
			sp[2] = (byte)((uint)i >> 8);
			sp[3] = (byte)((uint)i >> 0);
		}

		/**
        * fill len bytes of the output buffer with bytes Generated from
        * the derivation function.
        *
        * @throws DataLengthException if the out buffer is too small.
        */
		public int GenerateBytes(
			byte[] output,
			int outOff,
			int length)
		{
			if((output.Length - length) < outOff)
			{
				throw new DataLengthException("output buffer too small");
			}

			byte[] hashBuf = new byte[hLen];
			byte[] C = new byte[4];
			int counter = 0;

			digest.Reset();

			if(length > hLen)
			{
				do
				{
					ItoOSP(counter, C);

					digest.BlockUpdate(seed, 0, seed.Length);
					digest.BlockUpdate(C, 0, C.Length);
					digest.DoFinal(hashBuf, 0);

					Array.Copy(hashBuf, 0, output, outOff + counter * hLen, hLen);
				}
				while(++counter < (length / hLen));
			}

			if((counter * hLen) < length)
			{
				ItoOSP(counter, C);

				digest.BlockUpdate(seed, 0, seed.Length);
				digest.BlockUpdate(C, 0, C.Length);
				digest.DoFinal(hashBuf, 0);

				Array.Copy(hashBuf, 0, output, outOff + counter * hLen, length - (counter * hLen));
			}

			return length;
		}
	}

}