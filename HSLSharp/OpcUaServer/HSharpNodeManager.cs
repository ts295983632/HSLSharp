﻿using HslCommunication.LogNet;
using Opc.Ua;
using Opc.Ua.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.IO;
using HSLSharp.Configuration;
using HslCommunication.Core.Net;
using HSLSharp.Device;

namespace HSLSharp.OpcUaSupport
{
    /// <summary>
    /// Node manager 
    /// </summary>
    public class HSharpNodeManager : NodeManagerBase
    {

        #region Constructor
        
        public HSharpNodeManager(IServerInternal server, ApplicationConfiguration configuration)
            : base(server, configuration)
        {
            logNet = Util.LogNet;

            deviceCores = new List<IDeviceCore>( );
        }
        
        #endregion
        
        #region IDisposable Members

        /// <summary>
        /// An overrideable version of the Dispose.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // TBD
            }
        }
        #endregion

        #region INodeIdFactory Members

        /// <summary>
        /// Creates the NodeId for the specified node.
        /// </summary>
        public override NodeId New(ISystemContext context, NodeState node)
        {
            return node.NodeId;
        }

        #endregion
        
        #region INodeManager Members
        
        /// <summary>
        /// 在地址空间有用之前进行一些初始化的操作
        /// </summary>
        /// <remarks>
        /// The externalReferences is an out parameter that allows the node manager to link to nodes
        /// in other node managers. For example, the 'Objects' node is managed by the CoreNodeManager and
        /// should have a reference to the root folder node(s) exposed by this node manager.  
        /// </remarks>
        public override void CreateAddressSpace(IDictionary<NodeId, IList<IReference>> externalReferences)
        {
            lock (Lock)
            {
                LoadPredefinedNodes(SystemContext, externalReferences);

                IList<IReference> references = null;
                if (!externalReferences.TryGetValue(ObjectIds.ObjectsFolder, out references))
                {
                    externalReferences[ObjectIds.ObjectsFolder] = references = new List<IReference>();
                }



                // 从Xml文件进行加载数据
                logNet?.WriteInfo( "正在从资源加载节点信息..." );
                if (!File.Exists( Util.SharpSettings.NodeSettingsFilePath ))
                {
                    logNet?.WriteWarn( "配置文件不存在。加载结束。" );
                    return;
                }

                XElement element = XElement.Load( Util.SharpSettings.NodeSettingsFilePath );

                // 开始寻找设备信息，并计算一些节点信息


                AddNodeClass( null, element, references );




                //FolderState rootModbusAlien = CreateFolder( null, "ModbusAlien" );
                //rootModbusAlien.AddReference( ReferenceTypes.Organizes, true, ObjectIds.ObjectsFolder );
                //references.Add( new NodeStateReference( ReferenceTypes.Organizes, false, rootModbusAlien.NodeId ) );
                //rootModbusAlien.EventNotifier = EventNotifiers.SubscribeToEvents;
                //AddRootNotifier( rootModbusAlien );

                //// 解析异形服务器
                //foreach (var serverXml in element.Elements( ))
                //{
                //    if (serverXml.Attribute( "Name" ).Value == "ModbusAlien")
                //    {
                //        foreach (var alien in serverXml.Elements( "AlienNode" ))
                //        {
                //            AlienNode alienNode = new AlienNode( );
                //            alienNode.LoadByXmlElement( alien );
                //            FolderState rootAlien = CreateFolder( rootModbusAlien, alienNode.Name, alienNode.Description );


                //        }
                //    }
                //}


                //AddPredefinedNode( SystemContext, rootModbusAlien );

                // 构建数据
                //FolderState rootMy = CreateFolder(null,  "Modbus");
                //rootMy.AddReference(ReferenceTypes.Organizes, true, ObjectIds.ObjectsFolder);
                //references.Add(new NodeStateReference(ReferenceTypes.Organizes, false, rootMy.NodeId));
                //rootMy.EventNotifier = EventNotifiers.SubscribeToEvents;
                //AddRootNotifier(rootMy);




                //foreach (var node in Util.NodeSettings.Nodes)
                //{
                //var dataVariableState = CreateBaseVariable( rootMy, node.NodeName, node.NodeDescription, DataTypeIds.Byte, ValueRanks.OneDimension, new Byte[node.DataLength] );

                //dict_BaseDataVariableState.Add( dataVariableState.NodeId.ToString( ), dataVariableState );
                //}
                //AddPredefinedNode(SystemContext, rootMy);
            }
        }

        private void AddNodeClass( NodeState parent, XElement nodeClass, IList<IReference> references )
        {
            foreach(var xmlNode in nodeClass.Elements())
            {
                if (xmlNode.Name == "NodeClass")
                {
                    Configuration.NodeClass nClass = new Configuration.NodeClass( );
                    nClass.LoadByXmlElement( xmlNode );

                    FolderState son;
                    if (parent == null)
                    {
                        son = CreateFolder( null, nClass.Name );
                        son.Description = nClass.Description;
                        son.AddReference( ReferenceTypes.Organizes, true, ObjectIds.ObjectsFolder );
                        references.Add( new NodeStateReference( ReferenceTypes.Organizes, false, son.NodeId ) );
                        son.EventNotifier = EventNotifiers.SubscribeToEvents;
                        AddRootNotifier( son );


                        AddNodeClass( son, xmlNode, references );
                        AddPredefinedNode( SystemContext, son );
                    }
                    else
                    {
                        son = CreateFolder( parent, nClass.Name, nClass.Description );
                        AddNodeClass( son, xmlNode, references );
                    }
                }
                else if (xmlNode.Name == "DeviceNode")
                {
                    DeviceNode deviceNode = new DeviceNode( );
                    deviceNode.LoadByXmlElement( xmlNode );

                    FolderState son = CreateFolder( parent, deviceNode.Name, deviceNode.Description );
                }
                else if (xmlNode.Name == "AlienNode")
                {
                    AlienNode alienNode = new AlienNode( );
                    alienNode.LoadByXmlElement( xmlNode );

                    FolderState son = CreateFolder( parent, alienNode.Name, alienNode.Description );
                    AddNodeClass( son, xmlNode, references );
                }

            }
        }



        private void AddDeviceCore( NodeState parent, XElement device )
        {
            if (device.Name == "DeviceNode")
            {
                int deviceType = int.Parse(device.Attribute( "DeviceNode" ).Value);
                if(deviceType == DeviceNode.ModbusTcpAlien)
                {
                    ModbusTcpAline modbusTcpAline = new ModbusTcpAline( );
                    modbusTcpAline.LoadByXmlElement( device );

                    FolderState deviceFolder = CreateFolder( parent, modbusTcpAline.Name, modbusTcpAline.Description );

                    // 添加Request
                    foreach(var requestXml in device.Elements( "DeviceRequest" ))
                    {
                        DeviceRequest deviceRequest = new DeviceRequest( );
                        deviceRequest.LoadByXmlElement( requestXml );


                        AddDeviceRequest( deviceFolder, deviceRequest );
                    }

                    // 添加真实的设备
                    DeciveModbusTcpAlien deviceReal = new DeciveModbusTcpAlien( modbusTcpAline.DTU, device );
                    deviceReal.OpcUaNode = deviceFolder.ToString( );
                    deviceCores.Add( deviceReal );
                }
            }
        }


        private void AddDeviceRequest( NodeState parent , DeviceRequest deviceRequest)
        {
            // 提炼真正的数据节点
            List<RegularNode> regularNodes = Util.SharpRegulars.GetRegularNodes( deviceRequest.PraseRegularCode );
            foreach (var regularNode in regularNodes)
            {
                if (regularNode.RegularCode == RegularNodeTypeItem.Bool.Code)
                {
                    if (regularNode.TypeLength == 1)
                    {
                        var dataVariableState = CreateBaseVariable( parent, regularNode.Name, regularNode.Description, DataTypeIds.Boolean, ValueRanks.Scalar, default(bool) );
                        dict_BaseDataVariableState.Add( dataVariableState.NodeId.ToString( ), dataVariableState );
                    }
                    else
                    {
                        var dataVariableState = CreateBaseVariable( parent, regularNode.Name, regularNode.Description, DataTypeIds.Boolean, ValueRanks.OneDimension, new bool[regularNode.TypeLength] );
                        dict_BaseDataVariableState.Add( dataVariableState.NodeId.ToString( ), dataVariableState );
                    }
                }
                else if(regularNode.RegularCode == RegularNodeTypeItem.Byte.Code)
                {
                    if (regularNode.TypeLength == 1)
                    {
                        var dataVariableState = CreateBaseVariable( parent, regularNode.Name, regularNode.Description, DataTypeIds.Byte, ValueRanks.Scalar, default( byte ) );
                        dict_BaseDataVariableState.Add( dataVariableState.NodeId.ToString( ), dataVariableState );
                    }
                    else
                    {
                        var dataVariableState = CreateBaseVariable( parent, regularNode.Name, regularNode.Description, DataTypeIds.Byte, ValueRanks.OneDimension, new byte[regularNode.TypeLength] );
                        dict_BaseDataVariableState.Add( dataVariableState.NodeId.ToString( ), dataVariableState );
                    }
                }
                else if (regularNode.RegularCode == RegularNodeTypeItem.Int16.Code)
                {
                    if (regularNode.TypeLength == 1)
                    {
                        var dataVariableState = CreateBaseVariable( parent, regularNode.Name, regularNode.Description, DataTypeIds.Int16, ValueRanks.Scalar, default( short ) );
                        dict_BaseDataVariableState.Add( dataVariableState.NodeId.ToString( ), dataVariableState );
                    }
                    else
                    {
                        var dataVariableState = CreateBaseVariable( parent, regularNode.Name, regularNode.Description, DataTypeIds.Int16, ValueRanks.OneDimension, new short[regularNode.TypeLength] );
                        dict_BaseDataVariableState.Add( dataVariableState.NodeId.ToString( ), dataVariableState );
                    }
                }
                else if (regularNode.RegularCode == RegularNodeTypeItem.UInt16.Code)
                {
                    if (regularNode.TypeLength == 1)
                    {
                        var dataVariableState = CreateBaseVariable( parent, regularNode.Name, regularNode.Description, DataTypeIds.UInt16, ValueRanks.Scalar, default( ushort ) );
                        dict_BaseDataVariableState.Add( dataVariableState.NodeId.ToString( ), dataVariableState );
                    }
                    else
                    {
                        var dataVariableState = CreateBaseVariable( parent, regularNode.Name, regularNode.Description, DataTypeIds.UInt16, ValueRanks.OneDimension, new ushort[regularNode.TypeLength] );
                        dict_BaseDataVariableState.Add( dataVariableState.NodeId.ToString( ), dataVariableState );
                    }
                }
                else if (regularNode.RegularCode == RegularNodeTypeItem.Int32.Code)
                {
                    if (regularNode.TypeLength == 1)
                    {
                        var dataVariableState = CreateBaseVariable( parent, regularNode.Name, regularNode.Description, DataTypeIds.Int32, ValueRanks.Scalar, default( int ) );
                        dict_BaseDataVariableState.Add( dataVariableState.NodeId.ToString( ), dataVariableState );
                    }
                    else
                    {
                        var dataVariableState = CreateBaseVariable( parent, regularNode.Name, regularNode.Description, DataTypeIds.Int32, ValueRanks.OneDimension, new int[regularNode.TypeLength] );
                        dict_BaseDataVariableState.Add( dataVariableState.NodeId.ToString( ), dataVariableState );
                    }
                }
                else if (regularNode.RegularCode == RegularNodeTypeItem.UInt32.Code)
                {
                    if (regularNode.TypeLength == 1)
                    {
                        var dataVariableState = CreateBaseVariable( parent, regularNode.Name, regularNode.Description, DataTypeIds.UInt32, ValueRanks.Scalar, default( uint ) );
                        dict_BaseDataVariableState.Add( dataVariableState.NodeId.ToString( ), dataVariableState );
                    }
                    else
                    {
                        var dataVariableState = CreateBaseVariable( parent, regularNode.Name, regularNode.Description, DataTypeIds.UInt32, ValueRanks.OneDimension, new uint[regularNode.TypeLength] );
                        dict_BaseDataVariableState.Add( dataVariableState.NodeId.ToString( ), dataVariableState );
                    }
                }
                else if (regularNode.RegularCode == RegularNodeTypeItem.Float.Code)
                {
                    if (regularNode.TypeLength == 1)
                    {
                        var dataVariableState = CreateBaseVariable( parent, regularNode.Name, regularNode.Description, DataTypeIds.Float, ValueRanks.Scalar, default( float ) );
                        dict_BaseDataVariableState.Add( dataVariableState.NodeId.ToString( ), dataVariableState );
                    }
                    else
                    {
                        var dataVariableState = CreateBaseVariable( parent, regularNode.Name, regularNode.Description, DataTypeIds.Float, ValueRanks.OneDimension, new float[regularNode.TypeLength] );
                        dict_BaseDataVariableState.Add( dataVariableState.NodeId.ToString( ), dataVariableState );
                    }
                }
                else if (regularNode.RegularCode == RegularNodeTypeItem.Int64.Code)
                {
                    if (regularNode.TypeLength == 1)
                    {
                        var dataVariableState = CreateBaseVariable( parent, regularNode.Name, regularNode.Description, DataTypeIds.Int64, ValueRanks.Scalar, default( long ) );
                        dict_BaseDataVariableState.Add( dataVariableState.NodeId.ToString( ), dataVariableState );
                    }
                    else
                    {
                        var dataVariableState = CreateBaseVariable( parent, regularNode.Name, regularNode.Description, DataTypeIds.Int64, ValueRanks.OneDimension, new long[regularNode.TypeLength] );
                        dict_BaseDataVariableState.Add( dataVariableState.NodeId.ToString( ), dataVariableState );
                    }
                }
                else if (regularNode.RegularCode == RegularNodeTypeItem.UInt64.Code)
                {
                    if (regularNode.TypeLength == 1)
                    {
                        var dataVariableState = CreateBaseVariable( parent, regularNode.Name, regularNode.Description, DataTypeIds.UInt64, ValueRanks.Scalar, default( ulong ) );
                        dict_BaseDataVariableState.Add( dataVariableState.NodeId.ToString( ), dataVariableState );
                    }
                    else
                    {
                        var dataVariableState = CreateBaseVariable( parent, regularNode.Name, regularNode.Description, DataTypeIds.UInt64, ValueRanks.OneDimension, new ulong[regularNode.TypeLength] );
                        dict_BaseDataVariableState.Add( dataVariableState.NodeId.ToString( ), dataVariableState );
                    }
                }
                else if (regularNode.RegularCode == RegularNodeTypeItem.Double.Code)
                {
                    if (regularNode.TypeLength == 1)
                    {
                        var dataVariableState = CreateBaseVariable( parent, regularNode.Name, regularNode.Description, DataTypeIds.Double, ValueRanks.Scalar, default( double ) );
                        dict_BaseDataVariableState.Add( dataVariableState.NodeId.ToString( ), dataVariableState );
                    }
                    else
                    {
                        var dataVariableState = CreateBaseVariable( parent, regularNode.Name, regularNode.Description, DataTypeIds.Double, ValueRanks.OneDimension, new double[regularNode.TypeLength] );
                        dict_BaseDataVariableState.Add( dataVariableState.NodeId.ToString( ), dataVariableState );
                    }
                }
                else if (regularNode.RegularCode == RegularNodeTypeItem.StringAscii.Code ||
                    regularNode.RegularCode == RegularNodeTypeItem.StringUnicode.Code ||
                    regularNode.RegularCode == RegularNodeTypeItem.StringUtf8.Code)
                {

                    var dataVariableState = CreateBaseVariable( parent, regularNode.Name, regularNode.Description, DataTypeIds.String, ValueRanks.OneDimension, string.Empty );
                        dict_BaseDataVariableState.Add( dataVariableState.NodeId.ToString( ), dataVariableState );
                }
            }
            
        }


        #endregion
        


        #region Dictionary Resources




        #endregion

        #region Private Member

        private ILogNet logNet;
        private List<IDeviceCore> deviceCores;                     // 所有的设备客户端列表


        #endregion

        #region ModbusAlien


        private List<NetworkAlienClient> networkAliens;            // 所有的异形客户端的列表


        #endregion

    }
}
