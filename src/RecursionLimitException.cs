using System;
using System.IO;
using System.Collections.Generic;

namespace Mrasm
{
	public class RecursionLimitException : Exception
	{
		public RecursionLimitException(){}
	    public RecursionLimitException(string message) : base(message){}
	    public RecursionLimitException(string message, Exception inner) : base(message, inner){}
	}
}
