﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HSLSharp.Configuration
{
    /// <summary>
    /// Modbus服务器的数据信息，也就是说本系统支持多个服务器搭建
    /// </summary>
    public class ModbusNode
    {
        /// <summary>
        /// 服务器的名称
        /// </summary>
        public string Name { get; set; }


        /// <summary>
        /// 当前主站的端口号信息
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// 服务器的创建日期
        /// </summary>
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// 主站的描述信息
        /// </summary>
        public string Description { get; set; }
    }
}
