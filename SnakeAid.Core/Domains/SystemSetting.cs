using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SnakeAid.Core.Domains
{
    public class SystemSetting
    {
        public string SettingKey { get; set; }
        public string Value { get; set; }
        public string Description { get; set; }
        public SettingValueType ValueType { get; set; }
    }

    public enum SettingValueType
    {
        String = 0,
        Int = 1,
        Decimal = 2,
        Boolean = 3,
        Json = 4
    }
}