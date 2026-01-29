using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace SnakeAid.Core.Enums
{
    public enum RescuerType
    {
        [EnumMember(Value = "EMERGENCY")]
        Emergency = 0,

        [EnumMember(Value = "SNAKE_CATCHING")]
        Catching = 1,

        [EnumMember(Value = "Both")]
        Both = 2
    }
}
