﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HSLSharp.Configuration
{
    public class ModbusRequest : NodeClass
    {
        
        /// <summary>
        /// 客户端的站号
        /// </summary>
        public int Station { get; set; }

        /// <summary>
        /// 写入数据的地址，如果为负数，则忽略
        /// </summary>
        public int Address { get; set; }

        /// <summary>
        /// 读取的数据长度
        /// </summary>
        public ushort Length { get; set; }

        /// <summary>
        /// 本次请求解析字节数据的规则
        /// </summary>
        public string PraseRegularCode { get; set; }
        
        

    }
}
