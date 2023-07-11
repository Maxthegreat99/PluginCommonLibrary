using System;
using System.Runtime.Serialization;

namespace Terraria.Plugins.Common.Test {
  [Serializable]
  public class AssertException: Exception {
    protected AssertException(SerializationInfo info, StreamingContext context) { }
    public AssertException(string message, Exception inner): base(message, inner) {}
    public AssertException(string message): base(message, null) {}
    public AssertException(): base("Assert failed.") {}
  }
}
