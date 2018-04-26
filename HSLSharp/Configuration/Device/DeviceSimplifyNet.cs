﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace HSLSharp.Configuration
{
    /// <summary>
    /// SimplifyNet的设备信息
    /// </summary>
    public class DeviceSimplifyNet : DeviceNode
    {
        #region Constructor

        /// <summary>
        /// 使用默认的参数实例化一个设备信息
        /// </summary>
        public DeviceSimplifyNet( )
        {
            Name = "SimplifyNet客户端";
            Description = "设备用途的数据";
            DeviceType = DeviceNode.SimplifyNet;

            IpAddress = "127.0.0.1";
            Port = 6000;

            Token = Guid.Empty;
        }


        #endregion

        #region Public Properties


        /// <summary>
        /// 设备的Ip地址
        /// </summary>
        public string IpAddress { get; set; }


        /// <summary>
        /// 设备的端口号
        /// </summary>
        public int Port { get; set; }


        /// <summary>
        /// Token网络令牌
        /// </summary>
        public Guid Token { get; set; } 


        #endregion

        #region Xml Interface

        public override void LoadByXmlElement( XElement element )
        {
            base.LoadByXmlElement( element );
            IpAddress = element.Attribute( "IpAddress" ).Value;
            Port = int.Parse( element.Attribute( "Port" ).Value );
            Token = new Guid( element.Attribute( "Token" ).Value );
        }

        public override XElement ToXmlElement( )
        {
            XElement element = base.ToXmlElement( );
            element.SetAttributeValue( "IpAddress", IpAddress );
            element.SetAttributeValue( "Port", Port );
            element.SetAttributeValue( "Token", Token.ToString( "N" ) );
            return element;
        }

        #endregion

        #region Overide Method

        public override List<NodeClassRenderItem> GetNodeClassRenders( )
        {
            var list = base.GetNodeClassRenders( );
            list.Add( NodeClassRenderItem.CreateIpAddress( IpAddress ) );
            list.Add( NodeClassRenderItem.CreateIpPort( Port ) );
            list.Add( NodeClassRenderItem.CreateCustomer( "令牌", Token.ToString( "N" ) ) );

            return list;
        }


        #endregion


    }
}
