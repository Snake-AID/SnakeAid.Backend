using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace SnakeAid.Core.Enums
{
    public enum RegisterRole
    {
        [EnumMember(Value = "MEMBER")]
        Member = 0,

        [EnumMember(Value = "RESCUER")]
        Rescuer = 1,

        [EnumMember(Value = "EXPERT")]
        Expert = 2
    }
}
