using System;
using System.Text;
using System.Runtime.InteropServices;
namespace MT
{ 

}
public class MT_API
{
    //初始化
    [DllImport("MT_API.dll",CharSet=CharSet.Ansi,CallingConvention=CallingConvention.StdCall)]
    public static extern int MT_Init();

    [DllImport("MT_API.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    public static extern int MT_DeInit();

    [DllImport("MT_API.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    public static extern int MT_Get_Dll_Version(ref String sVer);
    
    //通信端口
    [DllImport("MT_API.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    public static extern int MT_Close_UART();

    [DllImport("MT_API.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    public static extern int MT_Close_USB();

    [DllImport("MT_API.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    public static extern int MT_Open_USB();

    [DllImport("MT_API.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    public static extern int MT_Open_UART(string sCOM);


    //握手
    [DllImport("MT_API.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    public static extern int MT_Check();
    //硬件信息
    [DllImport("MT_API.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    public static extern int MT_Get_Product_Resource(ref Int32 Value);
    [DllImport("MT_API.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    public static extern int MT_Get_Product_ID(ref String sID);
    [DllImport("MT_API.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    public static extern int MT_Get_Product_SN(ref String sSN);
    [DllImport("MT_API.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    public static extern int MT_Get_Product_Version(ref Int32 Major,ref Int32 Minor);
    //电机参数
    [DllImport("MT_API.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    public static extern int MT_Get_Axis_Num(ref Int32 Value);

    [DllImport("MT_API.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    public static extern int MT_Get_Axis_Acc(UInt16 AObj, ref Int32 Value);

    [DllImport("MT_API.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    public static extern int MT_Set_Axis_Acc(UInt16 AObj,Int32 Value);

    [DllImport("MT_API.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    public static extern int MT_Get_Axis_Dec(UInt16 AObj, ref Int32 Value);

    [DllImport("MT_API.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    public static extern int MT_Set_Axis_Dec(UInt16 AObj, Int32 Value);

    //运动模式
    [DllImport("MT_API.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    public static extern int MT_Set_Axis_Mode_Velocity(UInt16 AObj);

   
    [DllImport("MT_API.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    public static extern int MT_Set_Axis_Mode_Position(UInt16 AObj);

    [DllImport("MT_API.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    public static extern int MT_Set_Axis_Mode_Home(UInt16 AObj);


    //运动状态
    [DllImport("MT_API.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    public static extern int MT_Get_Axis_V_Now(UInt16 AObj, ref Int32 Value);

    [DllImport("MT_API.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    public static extern int MT_Set_Axis_Software_P_Now(UInt16 AObj, Int32 Value);

    [DllImport("MT_API.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    public static extern int MT_Get_Axis_Status(ushort AObj,
            ref Byte Run, ref Byte Dir, ref Byte Neg, ref Byte Pos, ref Byte Zero, ref Byte Mode);

    //速度模式
    [DllImport("MT_API.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    public static extern int MT_Get_Axis_Velocity_V_Target(UInt16 AObj, ref Int32 Value);

    [DllImport("MT_API.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    public static extern int MT_Set_Axis_Velocity_V_Target_Abs(UInt16 AObj, Int32 Value);

    [DllImport("MT_API.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    public static extern int MT_Set_Axis_Velocity_V_Target_Ref(UInt16 AObj, Int32 Value);

    [DllImport("MT_API.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    public static extern int MT_Set_Axis_Velocity_Stop(UInt16 AObj);

    //位置模式
    [DllImport("MT_API.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    public static extern int MT_Get_Axis_Position_V_Max(UInt16 AObj, ref Int32 Value);

    [DllImport("MT_API.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    public static extern int MT_Set_Axis_Position_V_Max(UInt16 AObj, Int32 Value);

    [DllImport("MT_API.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    public static extern int MT_Get_Axis_Position_P_Target(UInt16 AObj, ref Int32 Value);
    
    [DllImport("MT_API.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    public static extern int MT_Set_Axis_Position_P_Target_Abs(UInt16 AObj,Int32 Value);

    [DllImport("MT_API.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    public static extern int MT_Set_Axis_Position_P_Target_Rel(UInt16 AObj,Int32 Value);

    [DllImport("MT_API.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    public static extern int MT_Set_Axis_Position_Stop(UInt16 AObj);
    //软件限位
    [DllImport("MT_API.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    public static extern int MT_Set_Axis_Software_Limit_Neg_Value(UInt16 AObj, Int32 Value);

    [DllImport("MT_API.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    public static extern int MT_Set_Axis_Software_Limit_Pos_Value(UInt16 AObj, Int32 Value);

    [DllImport("MT_API.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    public static extern int MT_Set_Axis_Software_Limit_Enable(UInt16 AObj);

    [DllImport("MT_API.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    public static extern int MT_Set_Axis_Software_Limit_Disable(UInt16 AObj);

//零位模式
    [DllImport("MT_API.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    public static extern int MT_Set_Axis_Home_V(UInt16 AObj, Int32 Value);

    [DllImport("MT_API.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    public static extern int MT_Set_Axis_Home_Stop(UInt16 AObj);

    //紧急停止
    [DllImport("MT_API.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    public static extern int MT_Set_Axis_Halt(UInt16 AObj);

    [DllImport("MT_API.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    public static extern int MT_Set_Axis_Halt_All();

    //存储器操作
//    [DllImport("MT_API.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
 //   public static extern int MT_Get_Axis_Num(ref Int32 Value);

    [DllImport("MT_API.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    public static extern int MT_Set_Param_Mem_Data(UInt16 AObj, Byte Value);

    [DllImport("MT_API.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    public static extern int MT_Get_Param_Mem_Data(UInt16 AObj, ref Byte Value);

    //光隔输入
    [DllImport("MT_API.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    public static extern int MT_Get_Optic_In_Num(ref Int32 Value);

    [DllImport("MT_API.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    public static extern int MT_Get_Optic_In_Single(UInt16 AObj, ref Int32 Value);

    [DllImport("MT_API.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    public static extern int MT_Get_Optic_In_All(ref Int32 Value);
    //光隔输出
    [DllImport("MT_API.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    public static extern int MT_Get_Optic_Out_Num(ref Int32 Value);

    [DllImport("MT_API.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    public static extern int MT_Set_Optic_Out_Single(UInt16 AObj, Int32 Value);

    [DllImport("MT_API.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    public static extern int MT_Set_Optic_Out_All(Int32 Value);
}
