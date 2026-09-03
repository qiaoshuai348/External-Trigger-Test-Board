//////////////////////////////////////////////////////////////////////
////                                                              ////
//// i2cSlave.v                                                   ////
////                                                              ////
//// This file is part of the i2cSlave opencores effort.
//// <http://www.opencores.org/cores//>                           ////
////                                                              ////
//// Module Description:                                          ////
//// You will need to modify this file to implement your 
//// interface.
////                                                              ////
//// To Do:                                                       ////
//// 
////                                                              ////
//// Author(s):                                                   ////
//// - Steve Fielding, sfielding@base2designs.com                 ////
////                                                              ////
//////////////////////////////////////////////////////////////////////
////                                                              ////
//// Copyright (C) 2008 Steve Fielding and OPENCORES.ORG          ////
////                                                              ////
//// This source file may be used and distributed without         ////
//// restriction provided that this copyright statement is not    ////
//// removed from the file and that any derivative work contains  ////
//// the original copyright notice and the associated disclaimer. ////
////                                                              ////
//// This source file is free software; you can redistribute it   ////
//// and/or modify it under the terms of the GNU Lesser General   ////
//// Public License as published by the Free Software Foundation; ////
//// either version 2.1 of the License, or (at your option) any   ////
//// later version.                                               ////
////                                                              ////
//// This source is distributed in the hope that it will be       ////
//// useful, but WITHOUT ANY WARRANTY; without even the implied   ////
//// warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR      ////
//// PURPOSE. See the GNU Lesser General Public License for more  ////
//// details.                                                     ////
////                                                              ////
//// You should have received a copy of the GNU Lesser General    ////
//// Public License along with this source; if not, download it   ////
//// from <http://www.opencores.org/lgpl.shtml>                   ////
////                                                              ////
//////////////////////////////////////////////////////////////////////
//
`include "i2cSlave_define.v"


module i2cSlave (

input 	wire			clk			,
input 	wire			rst			,
input 	wire			sdaIn		,
input 	wire			scl			,
output 	wire			sdaOut  	,
output	wire 			sda_oe		,

input 	wire [7:0] 		myreg00		,
input 	wire [7:0] 		myreg01		,
input 	wire [7:0] 		myreg02		,
input 	wire [7:0] 		myreg03		,
input 	wire [7:0] 		myreg04		,                  		       		
input 	wire [7:0] 		myreg05		,
input 	wire [7:0] 		myreg06		,
     
          	           		       		
input 	wire [7:0] 		myreg28		, //
input 	wire [7:0] 		myreg29		,
input 	wire [7:0] 		myreg30		, //
input 	wire [7:0] 		myreg31		,
input 	wire [7:0] 		myreg32		, //
input 	wire [7:0] 		myreg33		,

input 	wire [7:0] 		myreg41		,
input 	wire [7:0] 		myreg42		,
                                      	
input   wire [7:0]		myreg52		,
input   wire [7:0]		myreg53	    ,
input   wire [7:0]		myreg54	    ,
input   wire [7:0]		myreg55	    ,
input   wire [7:0]		myreg56	    ,
     	           		
input 	wire [7:0] 		myreg200	, //FPGA verson year
input 	wire [7:0] 		myreg201	,
input 	wire [7:0] 		myreg202	,
input 	wire [7:0] 		myreg203	,
input 	wire [7:0] 		myreg204	,
input 	wire [7:0] 		myreg205	,
input 	wire [7:0] 		myreg206	,
input 	wire [7:0] 		myreg207	,
input	wire [7:0]		myreg210	,
  
  
output wire	 			reg00		,
output wire	 [1:0]		reg01		,
output wire				reg02		,
output wire				reg03		,
output wire				reg04		,
output wire				reg05		,
output wire				reg06		,
output wire				reg07		,
output wire				reg08		,
output wire   [7:0] 	reg09		,
output wire   [7:0] 	reg10		,
output wire   [7:0] 	reg11		,
output wire   [7:0] 	reg12		,
output wire   [7:0] 	reg13		,
output wire   [7:0] 	reg14		,
output wire   [7:0] 	reg15		,
output wire   [7:0] 	reg16		,
output wire   [7:0] 	reg17		,
output wire   [7:0] 	reg18		,
output wire   [7:0] 	reg19		,
output wire   [7:0] 	reg20		,
output wire   [7:0] 	reg21		,
output wire   [7:0] 	reg22		,
output wire   [7:0] 	reg23		,
output wire   [7:0] 	reg24		,
output wire   [7:0] 	reg25		,
output wire   [7:0] 	reg26		,
output wire   [7:0] 	reg27		,
output wire   [7:0] 	reg28		,
output wire   [7:0] 	reg29		,
output wire   			reg30		,
output wire   [7:0] 	reg31		,
output wire   [7:0] 	reg32		,
output wire   [7:0] 	reg33		,
output wire   			reg34		,
output wire   			reg35		,
output wire   			reg36		,
output wire   [7:0] 	reg37		,
output wire   [7:0] 	reg38		,
output wire   [7:0] 	reg39		,
output wire   [7:0] 	reg40		,
output wire   [7:0] 	reg41		,
output wire   [7:0] 	reg42		,
output wire   [7:0] 	reg43		,
output wire   [7:0] 	reg44		,
output wire   [7:0] 	reg45		,		
output wire   [7:0] 	reg46		,	

output wire  			reg49		,       //manual AMPV                           		                            		
output wire	[7:0] 		reg50		,
output wire	[7:0] 		reg51		,
output wire	[7:0] 		reg52		,
output wire	[7:0] 		reg53		,
output wire	[7:0] 		reg54		,
output wire	[7:0] 		reg55		,
output wire		  		reg56		,       //FRAME COUNTER ENABLE
output wire		  		reg57		,
output wire	[7:0] 		reg58		,
output wire	[7:0] 		reg59		,
output wire	[7:0] 		reg60		,
output wire	[7:0] 		reg61		,
output wire	[7:0] 		reg62		,
output wire	[0:0] 		reg63		,
output wire	[0:0] 		reg64		,
output wire	[7:0] 		reg65		,
output wire	[7:0] 		reg66		,
output wire	[7:0] 		reg67		,
output wire	[7:0] 		reg68		,
output wire	[0:0] 		reg69		,
output wire [7:0] 		reg70		,
output wire [7:0] 		reg71		,
output wire [7:0] 		reg72		,
output wire [7:0] 		reg73		,
output wire [7:0] 		reg74		,
output wire [7:0] 		reg75		,
output wire [1:0] 		reg76		,
output wire [1:0] 		reg77		,
output wire	[7:0]		reg79		,


output wire [7:0] 		reg80		,  //#1 PLL Dynamic Phase Adjustment C0-C4 Select
output wire 			reg81		,		   //direction
output wire 			reg82		,        //run
output wire 			reg83		,        //pll reset
output wire [7:0] 		reg84		,  //#2 PLL DPA C0-C4
output wire 			reg85		,        //direction
output wire 			reg86		,        //run
output wire 			reg87		,        //pll reset
output wire [7:0] 		reg88		,  //LVDS channel Select
output wire 			reg89		,

output wire [7:0] 		reg91		,  //LVDS input phase delay time
output wire 			reg92		,        //LVDS input phase delay time WR  0->1 action
output wire [7:0] 		reg93		,  //LVDS bit shift. bit select position
output wire 			reg94		,        //LVDS bit select WR  0->1 action
output wire 			reg95		,        //LVDS input phase detector exe  0:reset/clear  1:run  
output wire [7:0] 		reg96		,
output wire [0:0] 		reg97		,
output wire [0:0] 		reg98		,

output wire				reg1x03		,
output wire [7:0] 		reg1X05		,  //asmi_addr[31-
output wire [7:0] 		reg1X06		,  //asmi_addr[23-
output wire [7:0] 		reg1X07		,  //asmi-addr[15-
output wire [7:0] 		reg1X08		,  //asmi-addr[7-
output wire 			reg1X09		,         //Bulk_earse
output wire [7:0] 		reg1X10		,   //datain [7-]
output wire 			reg1X11		,
output wire 			reg1X12		,
output wire 			reg1X13		,
output wire 			reg1X14		,
output wire 			reg1X15		,
output wire 			reg1X16		,
output wire 			reg1X17		,
output wire 			reg1X18		,
output wire 			reg1X19		,
output wire 			reg1X20		,

output wire [7:0] 		reg1X29		,
output wire [7:0] 		reg1X30		,
output wire [7:0] 		reg1X31		,
output wire [7:0] 		reg1X32		,
output wire [7:0] 		reg1X33		,
output wire 			reg1X34		,
output wire [7:0] 		reg1X35		,
output wire 			reg1X36		,
output wire 			reg1X37		,
output wire 			reg1X38		,
output wire 			reg1X39		,
output wire 			reg1X40		,
output wire [7:0] 		reg1X41		,   
output wire				reg1X42		,
output wire	[7:0]		reg1X43		, 
output wire	[7:0]		reg1X46		,
output wire	[7:0]		reg1X47		,
output wire	[7:0] 		reg1X48		,
output wire [7:0] 		reg1X52 	,
output wire	[7:0]		reg1X53		,
output wire [7:0]		reg1X54		,
output wire [7:0] 		reg1X55		,
output wire [7:0] 		reg1X56		,     
output wire [7:0] 		reg1X57		,     
output wire [7:0] 		reg1X58		, 

output wire [7:0]     reg1X59,
output wire [7:0]     reg1x60 ,
output wire [7:0]     reg1x61 ,
output wire [7:0]     reg1x62 ,
output wire [7:0]     reg1x63 ,
output wire [7:0]     reg1x64 ,
output wire [7:0]     reg1x65 ,
output wire [7:0]     reg1x66,



output wire [7:0] 		reg2X55		,
output wire 			AwriteEn	,
output wire [7:0] 		AregAddr	,
output wire [7:0] 		AdataToRegIF


);


assign  sda_oe =  (sdaOut == 1'b0);//?1'b1 :1'b0;

// local wires and regs
reg sdaDeb;
reg sclDeb;
reg [`DEB_I2C_LEN-1:0] sdaPipe;
reg [`DEB_I2C_LEN-1:0] sclPipe;

reg [`SCL_DEL_LEN-1:0] sclDelayed;
reg [`SDA_DEL_LEN-1:0] sdaDelayed;
reg [1:0] startStopDetState;
wire clearStartStopDet;
//wire sdaOut;
//wire sdaIn;
wire [7:0] regAddr;
wire [7:0] dataToRegIF;
wire writeEn;
wire [7:0] dataFromRegIF;
reg [1:0] rstPipe;
wire rstSyncToClk;
reg startEdgeDet;

//assign sda = (sdaOut == 1'b0) ? 1'b0 : 1'bz;
//assign sdaIn = sda;

//output the writeEN,addr,dataToRegIF (dataIn) signal
assign AwriteEn = (reg2X55[0]==1'b1)?writeEn:1'b0;
assign AdataToRegIF = dataToRegIF;
assign AregAddr = regAddr;

// sync rst rsing edge to clk
always @(posedge clk) begin
  if (rst == 1'b1)
    rstPipe <= 2'b11;
  else
    rstPipe <= {rstPipe[0], 1'b0};
end

assign rstSyncToClk = rstPipe[1];

// debounce sda and scl
always @(posedge clk) begin
  if (rstSyncToClk == 1'b1) begin
    sdaPipe <= {`DEB_I2C_LEN{1'b1}};
    sdaDeb <= 1'b1;
    sclPipe <= {`DEB_I2C_LEN{1'b1}};
    sclDeb <= 1'b1;
  end
  else begin
    sdaPipe <= {sdaPipe[`DEB_I2C_LEN-2:0], sdaIn};
    sclPipe <= {sclPipe[`DEB_I2C_LEN-2:0], scl};
    if (&sclPipe[`DEB_I2C_LEN-1:1] == 1'b1)
      sclDeb <= 1'b1;
    else if (|sclPipe[`DEB_I2C_LEN-1:1] == 1'b0)
      sclDeb <= 1'b0;
    if (&sdaPipe[`DEB_I2C_LEN-1:1] == 1'b1)
      sdaDeb <= 1'b1;
    else if (|sdaPipe[`DEB_I2C_LEN-1:1] == 1'b0)
      sdaDeb <= 1'b0;
  end
end


// delay scl and sda
// sclDelayed is used as a delayed sampling clock
// sdaDelayed is only used for start stop detection
// Because sda hold time from scl falling is 0nS
// sda must be delayed with respect to scl to avoid incorrect
// detection of start/stop at scl falling edge. 
always @(posedge clk) begin
  if (rstSyncToClk == 1'b1) begin
    sclDelayed <= {`SCL_DEL_LEN{1'b1}};
    sdaDelayed <= {`SDA_DEL_LEN{1'b1}};
  end
  else begin
    sclDelayed <= {sclDelayed[`SCL_DEL_LEN-2:0], sclDeb};
    sdaDelayed <= {sdaDelayed[`SDA_DEL_LEN-2:0], sdaDeb};
  end
end

// start stop detection
always @(posedge clk) begin
  if (rstSyncToClk == 1'b1) begin
    startStopDetState <= `NULL_DET;
    startEdgeDet <= 1'b0;
  end
  else begin
    if (sclDeb == 1'b1 && sdaDelayed[`SDA_DEL_LEN-2] == 1'b0 && sdaDelayed[`SDA_DEL_LEN-1] == 1'b1)
      startEdgeDet <= 1'b1;
    else
      startEdgeDet <= 1'b0;
    if (clearStartStopDet == 1'b1)
      startStopDetState <= `NULL_DET;
    else if (sclDeb == 1'b1) begin
      if (sdaDelayed[`SDA_DEL_LEN-2] == 1'b1 && sdaDelayed[`SDA_DEL_LEN-1] == 1'b0) 
        startStopDetState <= `STOP_DET;
      else if (sdaDelayed[`SDA_DEL_LEN-2] == 1'b0 && sdaDelayed[`SDA_DEL_LEN-1] == 1'b1)
        startStopDetState <= `START_DET;
    end
  end
end


registerInterface u_registerInterface(
  .clk(clk),
  .addr(regAddr),
  .dataIn(dataToRegIF),
  .writeEn(writeEn),
  .dataOut(dataFromRegIF),
  .myreg00(myreg00),
  .myreg01(myreg01),
  .myreg02(myreg02),
  .myreg03(myreg03),
 
  .myreg05(myreg05),
  .myreg06(myreg06),
  //.myreg07(myreg07),
  .myreg28(myreg28),
  .myreg29(myreg29),
  .myreg30(myreg30),
  .myreg31(myreg31),
  .myreg32(myreg32),
  .myreg33(myreg33),  
  
  .myreg41(myreg41),
  .myreg42(myreg42),   
  
  .myreg52(myreg52	),	
  .myreg53(myreg53	),	
  .myreg54(myreg54	),	
  .myreg55(myreg55	),	
  .myreg56(myreg56	),	
             
  .myreg200(myreg200),
  .myreg201(myreg201),
  .myreg202(myreg202),
  .myreg203(myreg203),
  .myreg204(myreg204),
  .myreg205(myreg205),
  .myreg206(myreg206),  
  .myreg207(myreg207),
  .myreg210(myreg210),

  
  
  .reg00(reg00),
  .reg01(reg01),
  .reg02(reg02),
  .reg03(reg03),
  .reg04(reg04),
  .reg05(reg05),
  .reg06(reg06),
  .reg07(reg07),
  .reg08(reg08),
  .reg09(reg09),
  .reg10(reg10),
  .reg11(reg11),
  .reg12(reg12),
  .reg13(reg13),
  .reg14(reg14),
  .reg15(reg15),
  .reg16(reg16),
  .reg17(reg17),
  .reg18(reg18),
  .reg19(reg19),
  .reg20(reg20),
  .reg21(reg21),
  .reg22(reg22),
  .reg23(reg23),
  .reg24(reg24),
  .reg25(reg25),
  .reg26(reg26),
  .reg27(reg27),
  .reg28(reg28),
  .reg29(reg29),
  .reg30(reg30),
  .reg31(reg31),
  .reg32(reg32),
  .reg33(reg33),
  .reg34(reg34),
  .reg35(reg35),
  .reg36(reg36),
  .reg37(reg37),
  .reg38(reg38),
  .reg39(reg39),
  .reg40(reg40),
  .reg41(reg41),
  .reg42(reg42),
  .reg43(reg43),
  .reg44(reg44),
  .reg45(reg45),
  .reg46(reg46),
  .reg49(reg49),
  .reg50(reg50),
  .reg51(reg51),
  .reg52(reg52),
  .reg53(reg53),
  .reg54(reg54),
  .reg55(reg55),
  .reg56(reg56),
  .reg57(reg57),
  .reg58(reg58),
  .reg59(reg59),
  .reg60(reg60),
  .reg61(reg61),
  .reg62(reg62),
  .reg63(reg63),
  .reg64(reg64),
  .reg65(reg65),
  .reg66(reg66),
  .reg67(reg67),
  .reg68(reg68),
     
  .reg80(reg80),
  .reg81(reg81),
  .reg82(reg82),
  .reg83(reg83),
  .reg84(reg84),
  .reg85(reg85),
  .reg86(reg86),
  .reg87(reg87),
  .reg88(reg88),
  .reg89(reg89),
  .reg91(reg91),
  .reg92(reg92),
  .reg93(reg93),
  .reg94(reg94),
  .reg95(reg95),
  .reg96(reg96),
  
  .reg1x03(reg1x03),
  .reg1X05(reg1X05),  
  .reg1X06(reg1X06),
  .reg1X07(reg1X07),
  .reg1X08(reg1X08),
  .reg1X09(reg1X09),
  .reg1X10(reg1X10),
  .reg1X11(reg1X11),
  .reg1X12(reg1X12),
  .reg1X13(reg1X13),
  .reg1X14(reg1X14),
  .reg1X15(reg1X15),
  .reg1X16(reg1X16),
  .reg1X17(reg1X17),
  .reg1X18(reg1X18),
  .reg1X19(reg1X19),
  .reg1X20(reg1X20),
  
  .reg1X29(reg1X29),  
  .reg1X30(reg1X30),
  .reg1X31(reg1X31),
  .reg1X32(reg1X32),
  .reg1X33(reg1X33),
  .reg1X34(reg1X34),
  .reg1X35(reg1X35),
  .reg1X36(reg1X36),
  .reg1X37(reg1X37),
  .reg1X38(reg1X38),
  .reg1X39(reg1X39),
  .reg1X40(reg1X40),
  .reg1X41(reg1X41),   
  .reg1X42(reg1X42), 
  .reg1X43(reg1X43),
  .reg1X46(reg1X46 ),  
  .reg1X47(reg1X47 ),  
  .reg1X48(reg1X48 ),  
  
     
  .reg1X52(reg1X52),
  .reg1X53(reg1X53),   
  .reg1X54(reg1X54),
  .reg1X55(reg1X55),
  .reg1X56(reg1X56),
  .reg1X57(reg1X57),
  .reg1X58(reg1X58),

  .reg1X59( reg1X59),
  .reg1x60( reg1x60),
  .reg1x61( reg1x61),
  .reg1x62( reg1x62),
  .reg1x63( reg1x63),
  .reg1x64( reg1x64),
  .reg1x65( reg1x65),
  .reg1x66( reg1x66),
  
  .reg2X55(reg2X55),
  
  .myreg04(myreg04),
  .reg69(reg69),
  .reg70(reg70), 
  .reg71(reg71), 
  .reg72(reg72), 
  .reg73(reg73), 
  .reg74(reg74), 
  .reg75(reg75), 
  .reg76(reg76), 
  .reg77(reg77), 
  .reg79(reg79),
  .reg97(reg97), 
  .reg98(reg98)
);

serialInterface u_serialInterface (
  .clk(clk), 
  .rst(rstSyncToClk | startEdgeDet), 
  .dataIn(dataFromRegIF), 
  .dataOut(dataToRegIF), 
  .writeEn(writeEn),
  .regAddr(regAddr), 
  .scl(sclDelayed[`SCL_DEL_LEN-1]), 
  .sdaIn(sdaDeb), 
  .sdaOut(sdaOut), 
  .startStopDetState(startStopDetState),
  .clearStartStopDet(clearStartStopDet) 
);


endmodule


 
