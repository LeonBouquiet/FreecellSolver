using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace FreecellSolver
{
	/// <summary>
	/// Models an error situation that occured in the Freecell solver logic.
	/// </summary>
	[Serializable]
	public class SolverException: Exception
	{
		/// <summary>
		/// Default constructor.
		/// </summary>
		public SolverException()
		{
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="message">The error message</param>
		public SolverException(string message)
			: base(message)
		{
		}

		/// <summary>
		/// Deserialization constructor.
		/// </summary>
		protected SolverException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="message">The error message</param>
		/// <param name="innerException">The error that caused this SolverException.</param>
		public SolverException(string message, Exception innerException): 
			base(message, innerException)
		{
		}
	}
}
