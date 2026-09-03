//////////////////////////////////////////////////////////////////////
////                                                              ////
//// registerInterface.v                                          ////
////                                                              ////
//// This file is part of the i2cSlave opencores effort.
//// <http://www.opencores.org/cores//>                           ////
////                                                              ////
//// Module Description:                                          ////
//// You will need to modify this file to implement your 
//// interface.
//// Add your control and status bytes/bits to module inputs and outputs,
//// and also to the I2C read and write process blocks  
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


module registerInterface (
  
input clk,
input [7:0] addr,
input [7:0] dataIn,
input writeEn,
output reg [7:0] dataOut,  
input [7:0] myreg00,
input [7:0] myreg01,
input [7:0] myreg02,
input [7:0] myreg03,
input [7:0] myreg04,
input [7:0] myreg05,
input [7:0] myreg06,
//input [1:0] myreg07,
input [7:0] myreg28,
input [7:0] myreg29,
input [7:0] myreg30,
input [7:0] myreg31,
input [7:0] myreg32,
input [7:0] myreg33,

input [7:0] myreg41,
input [7:0] myreg42,

 input   wire [7:0]		myreg52		,      
 input   wire [7:0]		myreg53	    ,      
 input   wire [7:0]		myreg54	    ,      
 input   wire [7:0]		myreg55	    ,      
 input   wire [7:0]		myreg56	    ,      




input [7:0] myreg200,
input [7:0] myreg201,
input [7:0] myreg202,
input [7:0] myreg203,
input [7:0] myreg204,
input [7:0] myreg205,
input [7:0] myreg206,
input [7:0]	myreg207,
input [7:0]	myreg210,

output reg reg00,
output reg [1:0] reg01=2'b01,
output reg reg02=0,
output reg reg03,
output reg reg04,
output reg reg05,
output reg reg06,
output reg reg07,
output reg reg08,
output reg [7:0] reg09,
output reg [7:0] reg10,
output reg [7:0] reg11,
output reg [7:0] reg12,
output reg [7:0] reg13,
output reg [7:0] reg14,
output reg [7:0] reg15,
output reg [7:0] reg16,
output reg [7:0] reg17,
output reg [7:0] reg18,
output reg [7:0] reg19,
output reg [7:0] reg20,
output reg [7:0] reg21,
output reg [7:0] reg22,
output reg [7:0] reg23,
output reg [7:0] reg24,
output reg [7:0] reg25,
output reg [7:0] reg26,
output reg [7:0] reg27,
output reg [7:0] reg28,
output reg [7:0] reg29,
output reg reg30,
output reg [7:0] reg31,
output reg [7:0] reg32,
output reg [7:0] reg33,
output reg reg34,
output reg reg35,
output reg reg36,
output reg [7:0] reg37,
output reg [7:0] reg38,
output reg [7:0] reg39,
output reg [7:0] reg40,
output reg [7:0] reg41,
output reg [7:0] reg42,
output reg [7:0] reg43,
output reg [7:0] reg44,
output reg [7:0] reg45,
output reg [7:0] reg46,

output reg reg49,        //manual ampv


output reg [7:0] reg50,
output reg [7:0] reg51,
output reg [7:0] reg52,
output reg [7:0] reg53=0,
output reg [7:0] reg54=1,
output reg [7:0] reg55,
output reg reg56,        //FRAME COUNTER ENABLE
output reg reg57,
output reg [7:0] reg58,
output reg [7:0] reg59,
output reg [7:0] reg60,
output reg [7:0] reg61,
output reg [7:0] reg62,
output reg [0:0] reg63,
output reg [0:0] reg64,
output reg [7:0] reg65,
output reg [7:0] reg66,
output reg [7:0] reg67,
output reg [7:0] reg68,
output reg [0:0] reg69, 
output reg [7:0] reg70, 
output reg [7:0] reg71, 
output reg [7:0] reg72, 
output reg [7:0] reg73, 
output reg [7:0] reg74, 
output reg [7:0] reg75, 
output reg [1:0] reg76, 
output reg [1:0] reg77, 
output reg [7:0] reg79=0,// 

output reg [0:0] reg97, 
output reg [0:0] reg98, 

output reg [7:0] reg80,  //#1 PLL Dynamic Phase Adjustment C0-C4 Select
output reg reg81,		   //direction
output reg reg82,        //run
output reg reg83,        //pll reset
output reg [7:0] reg84,  //#2 PLL DPA C0-C4
output reg reg85,        //direction
output reg reg86,        //run
output reg reg87,        //pll reset
output reg [7:0] reg88,  //LVDS channel Select
output reg reg89 =1,
output reg [7:0] reg91,  //LVDS input phase delay time
output reg reg92,        //LVDS input phase delay time WR  0->1 action
output reg [7:0] reg93,  //LVDS bit shift. bit select position
output reg reg94,        //LVDS bit select WR  0->1 action
output reg reg95,         //LVDS input phase 
output reg [7:0] reg96,   //HDR mode  0= output both High Gain and low Gain  1=output low gain  2=output high gain
 
output reg		 reg1x03=0,
output reg [7:0] reg1X05,
output reg [7:0] reg1X06,
output reg [7:0] reg1X07,
output reg [7:0] reg1X08,
 
 
output reg reg1X09,         //Bulk_earse
output reg [7:0] reg1X10,   //datain [7-]
output reg reg1X11,
output reg reg1X12,
output reg reg1X13,
output reg reg1X14,
output reg reg1X15,
output reg reg1X16,
output reg reg1X17=1,
output reg reg1X18,
output reg reg1X19,
output reg reg1X20,

output reg [7:0] reg1X29, 
output reg [7:0] reg1X30,   //datain [7-]
output reg [7:0] reg1X31,
output reg [7:0] reg1X32,
output reg [7:0] reg1X33,
output reg reg1X34,
output reg [7:0] reg1X35,
output reg reg1X36,
output reg reg1X37,
output reg reg1X38,
output reg reg1X39,
output reg reg1X40,
output reg reg1X41,
output reg reg1X42=0,  
output reg [7:0] reg1X43=0,

output reg	[7:0]		reg1X46	=0	  	,     
output reg	[7:0]		reg1X47	=8'h04	,     
output reg	[7:0] 		reg1X48	=8'hE2	,     

 
output reg 		 reg1X52=0,    
output reg [7:0] reg1X53=0,

output reg [7:0] reg1X54	=0	,      
output reg [7:0] reg1X55	=0	,      
output reg [7:0] reg1X56=0	  ,     
output reg [7:0] reg1X57=8'h00,     
output reg [7:0] reg1X58=8'h00,   

output reg [7:0]     reg1X59,
output reg [7:0]     reg1x60 ,
output reg [7:0]     reg1x61 ,
output reg [7:0]     reg1x62 ,
output reg [7:0]     reg1x63 ,
output reg [7:0]     reg1x64 ,
output reg [7:0]     reg1x65 ,
output reg [7:0]     reg1x66,


output reg [7:0] reg2X55
  
);







initial
begin
	reg1X17<=1;
	reg00 <= 1'b0;
	reg01 <= 2'b01;
	reg02 <= 1'b0;
	reg03 <= 1'b0;
	reg04 <= 1'b0;
	reg05 <= 1'b0;
	reg06 <= 1'b0;
	reg07 <= 1'b0;
	reg08 <= 1'b0;
	reg09 <= 8'd0;
	reg10 <= 8'd0;
	reg11 <= 8'd0;
	reg12 <= 8'd0;
	reg13 <= 8'd0;
	reg14 <= 8'd0;
	reg15 <= 8'd0;
	reg16 <= 8'd0;
	reg17 <= 8'd0;
	reg18 <= 8'd8;
	reg19 <= 8'd8;
	reg20 <= 8'd8;
	reg21 <= 8'd8;
	reg22 <= 8'd0;
	reg23 <= 8'd0;
	reg24 <= 8'd0;
	reg25 <= 8'd0;
	reg26 <= 8'd0;
	reg27 <= 8'd0;
	reg28 <= 8'd0;
	reg29 <= 8'd0;
	reg30 <= 1'b0;
	reg31 <= 8'd0;
	reg32 <= 8'd00;
	reg33 <= 8'd0;
	reg34 <= 1'b0;
	reg35 <= 1'b0;
	reg36 <= 1'b0;
	reg37 <= 8'd0;
	reg38 <= 8'd2;
	reg39 <= 8'd0;
	reg40 <= 8'd0;
	reg41 <= 8'd0;
	reg42 <= 8'd0;
	reg43 <= 8'h1f;
	reg44 <= 8'h40;
	reg45 <= 8'h00;
	reg46 <= 8'h00;
	
	reg49 <= 8'h1;
	
	reg65 <= 8'h00; // [7:0]
	reg66 <= 8'h00; // [15:8]
	reg67 <= 8'h00; //
	reg68 <= 8'h1c; // 64Mbyte = 64*1024*1024= 0x0400_0000 byte; 512Mbyte= 512*1024*1024=0x2000_0000;;rema_num= (512-64)Mbyte=0x1c00_0000
	
    reg80 <= 0;
	reg81 <= 0;
	reg82 <= 0;
	reg83 <= 0;
	reg84 <= 0;
	reg85 <= 0;
	reg86 <= 0;
	reg87 <= 0;
	reg88 <= 0;
	reg89 <= 1;
	reg91 <= 0;
	reg92 <= 0;
	reg93 <= 0;
	reg94 <= 0;
	reg95 <= 0;
	reg96 <= 0;
	reg61 <= 0;
	reg1x03<=0;   
	reg1X42<=0;   
	reg1X43<=0; 
	
	reg1X46	<=0	  	;
	reg1X47	<=8'h04	;
	reg1X48	<=8'hE2	;//1250£º 1250*40ns = 50us
		
	reg1X52<=0;		
	reg1X53<=0  ;
	reg1X54<=0	;
	reg1X55<=0	;
	reg1X56<=0	;// 'h0004E2='D1250*40NS=50000NS 
	reg1X57<=0  ;//
	reg1X58<=0  ;//  
    reg1X59<=0 ;
    reg1x60<=0 ;
    reg1x61<=0 ;
    reg1x62<=0 ;
    reg1x63<=0 ;
    reg1x64<=0 ;
    reg1x65<=0 ;
    reg1x66<=0 ;
    
	
	reg2X55 <= 0;
	reg79<=0;

	
end

// --- I2C Read
always @(posedge clk) begin
  case (addr)
    8'h00: dataOut <= myreg00;
    8'h01: dataOut <= myreg01;
    8'h02: dataOut <= myreg02;
    8'h03: dataOut <= myreg03;
    8'h04: dataOut <= myreg04;
    8'h05: dataOut <= myreg05;
    8'h06: dataOut <= myreg06;	
   // 8'h07: dataOut <= myreg07;
	 8'd28: dataOut <= myreg28;
	 //8'd29: dataOut <= myreg29; 
	 //8'd30: dataOut <= myreg30;
	 //8'd31: dataOut <= myreg31;
	 //8'd32: dataOut <= myreg32;
 	 //8'd33: dataOut <= myreg33; 
 	 
 	 8'd41: dataOut <= myreg41;
 	 8'd42: dataOut <= myreg42;  
 	 
 	 
 	 8'd52: dataOut	<= myreg52	;
 	 8'd53: dataOut	<= myreg53	;
 	 8'd54: dataOut	<= myreg54	;
 	 8'd55: dataOut	<= myreg55	;
 	 8'd56: dataOut	<= myreg56	;
 	  	  	 	 	  	 	 
	 8'd200: dataOut <= myreg200;
	 8'd201: dataOut <= myreg201; 
	 8'd202: dataOut <= myreg202;
	 8'd203: dataOut <= myreg203;
	 8'd204: dataOut <= myreg204;
	 8'd205: dataOut <= myreg205;
	 8'd206: dataOut <= myreg206;	
	 8'd207: dataOut <= myreg207;
	 8'd210: dataOut <= myreg210;
	 
    default: dataOut <= 8'h00;
  endcase
end

// --- I2C Write



always @(posedge clk) 
begin
  if (writeEn == 1'b1) 
   begin
       if(addr==255) reg2X55<=dataIn;
		 else          reg2X55<=reg2X55;
   end  
end 

always @(posedge clk) begin
  if (writeEn == 1'b1 && reg2X55==0) begin
    case (addr)
      8'h00: reg00 <= dataIn[0];
      8'h01: reg01 <= dataIn[1:0];
      8'h02: reg02 <= dataIn[0];
      8'h03: reg03 <= dataIn[0];
      8'h04: reg04 <= dataIn[0];
      8'h05: reg05 <= dataIn[0];
      8'h06: reg06 <= dataIn[0];
      8'h07: reg07 <= dataIn[0];
		8'h08: reg08 <= dataIn[0];
		8'h09: reg09 <= dataIn;
		8'h0A: reg10 <= dataIn;
		8'h0B: reg11 <= dataIn;
		8'h0C: reg12 <= dataIn;
		8'h0D: reg13 <= dataIn;
		8'h0E: reg14 <= dataIn;
		8'h0F: reg15 <= dataIn;
		8'h10: reg16 <= dataIn;
		8'h11: reg17 <= dataIn;
		8'h12: reg18 <= dataIn;
		8'h13: reg19 <= dataIn;
		8'h14: reg20 <= dataIn;
		8'h15: reg21 <= dataIn;
		8'h16: reg22 <= dataIn;
		8'h17: reg23 <= dataIn;
		8'h18: reg24 <= dataIn;
		8'h19: reg25 <= dataIn;
		8'h1a: reg26 <= dataIn;
		8'h1b: reg27 <= dataIn;
		8'h1c: reg28 <= dataIn;
		8'h1d: reg29 <= dataIn;
		8'h1e: reg30 <= dataIn[0];
		8'h1f: reg31 <= dataIn;
		8'h20: reg32 <= dataIn;
		8'h21: reg33 <= dataIn;		
		8'h22: reg34 <= dataIn[0];
		8'h23: reg35 <= dataIn[0];		
		8'h24: reg36 <= dataIn[0];		
		8'h25: reg37 <= dataIn;
		8'h26: reg38 <= dataIn;	
		8'h27: reg39 <= dataIn;	
      8'h28: reg40 <= dataIn;	
      8'h29: reg41 <= dataIn;	
      8'h2a: reg42 <= dataIn;	
      8'h2b: reg43 <= dataIn;	
      8'h2c: reg44 <= dataIn;
		8'h2d: reg45 <= dataIn;
	   8'd46: reg46 <= dataIn;
		
		8'd49: reg49 <=dataIn;
		
		8'd50: reg50 <=dataIn;
		8'd51: reg51 <=dataIn;
		8'd52: reg52 <=dataIn;
		8'd53: reg53 <=dataIn;
		8'd54: reg54 <=dataIn;
		8'd55: reg55 <=dataIn;
		8'd56: reg56 <=dataIn[0];
		8'd57: reg57 <=dataIn[0];		
		8'd58: reg58 <=dataIn;		
		8'd59: reg59 <=dataIn;

		8'd60: reg60 <=dataIn;
		8'd61: reg61 <=dataIn;
		8'd62: reg62 <=dataIn;
		8'd63: reg63 <=dataIn[0];
		8'd64: reg64 <=dataIn[0];
		8'd65: reg65 <=dataIn;
		8'd66: reg66 <=dataIn;
		8'd67: reg67 <=dataIn;		
		8'd68: reg68 <=dataIn;		
		8'd69: reg69 <=dataIn[0];	 
		8'd70: reg70 <=dataIn; 
		8'd71: reg71 <=dataIn; 
		8'd72: reg72 <=dataIn; 
		8'd73: reg73 <=dataIn; 
		8'd74: reg74 <=dataIn; 
		8'd75: reg75 <=dataIn; 
		8'd76: reg76 <=dataIn[1:0]; 
		8'd77: reg77 <=dataIn[1:0]; 
		8'd97: reg97 <=dataIn[0]; 
		8'd98: reg98 <=dataIn[0]; 
		
		
		8'd79: reg79 <= dataIn;	
		8'd80: reg80 <= dataIn;
		8'd81: reg81 <= dataIn[0];
		8'd82: reg82 <= dataIn[0];
		8'd83: reg83 <= dataIn[0];
		8'd84: reg84 <= dataIn;
		8'd85: reg85 <= dataIn[0];
		8'd86: reg86 <= dataIn[0];
		8'd87: reg87 <= dataIn[0];
		8'd88: reg88 <= dataIn;
		8'd89: reg89 <= dataIn;
		8'd91: reg91 <= dataIn;
		8'd92: reg92 <= dataIn[0];
		8'd93: reg93 <= dataIn;
		8'd94: reg94 <= dataIn[0];
		8'd95: reg95 <= dataIn[0];
		8'd96: reg96 <= dataIn;
		
		8'd103: reg1x03<=dataIn;
		8'd105: reg1X05 <= dataIn;
		8'd106: reg1X06 <= dataIn;
		8'd107: reg1X07 <= dataIn;
		8'd108: reg1X08 <= dataIn;		
 
		8'd109: reg1X09 <= dataIn[0];
		8'd110: reg1X10 <= dataIn;
		8'd111: reg1X11 <= dataIn[0];
		8'd112: reg1X12 <= dataIn[0];
		8'd113: reg1X13 <= dataIn[0];
		8'd114: reg1X14 <= dataIn[0];
		8'd115: reg1X15 <= dataIn[0];
		8'd116: reg1X16 <= dataIn[0];
		8'd117: reg1X17 <= dataIn[0];
		8'd118: reg1X18 <= dataIn[0];
		8'd119: reg1X19 <= dataIn[0];
		8'd120: reg1X20 <= dataIn[0];	
		
		8'd129: reg1X29 <= dataIn;		
		8'd130: reg1X30 <= dataIn;
		8'd131: reg1X31 <= dataIn;
		8'd132: reg1X32 <= dataIn;
		8'd133: reg1X33 <= dataIn;
		8'd134: reg1X34 <= dataIn[0];
		8'd135: reg1X35 <= dataIn;
		8'd136: reg1X36 <= dataIn[0];
		8'd137: reg1X37 <= dataIn[0];
		8'd138: reg1X38 <= dataIn[0];
		8'd139: reg1X39 <= dataIn[0];
		8'd140: reg1X40 <= dataIn[0];	
		8'd141: reg1X41 <= dataIn;	
		8'd142: reg1X42 <= dataIn; 
		8'd143: reg1X43 <= dataIn;
		8'd146: reg1X46 <= dataIn; 
		8'd147: reg1X47 <= dataIn;
		8'd148: reg1X48 <= dataIn; 
		                          
		8'd152: reg1X52<=dataIn	 ;
		8'd153:	reg1X53<=dataIn	 ;   
		8'd154: reg1X54<=dataIn ;
		8'd155: reg1X55<=dataIn ;        
		8'd156:	reg1X56<=dataIn	 ;// 'h0004E2='D1250*40NS=50000NS 
		8'd157:	reg1X57<=dataIn  ;//
		8'd158:	reg1X58<=dataIn  ;// 

		8'd159: reg1X59<=dataIn ;
		8'd160: reg1x60<=dataIn ;
		8'd161: reg1x61<=dataIn ;
		8'd162: reg1x62<=dataIn ;
		8'd163: reg1x63<=dataIn ;
		8'd164: reg1x64<=dataIn ;
		8'd165: reg1x65<=dataIn ;
		8'd166: reg1x66<=dataIn ;
		
    endcase
  end
end

endmodule


 
