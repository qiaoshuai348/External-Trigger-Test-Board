`timescale 1ns / 1ps
//-------------------------------------------------------------------------------
// Company:  QHYCCD
// Engineer: YangSK
// 
// Create Date: 2022/7/12
// Design Name: T35_TOP
// Module Name: T35_TOP
// Project Name: T35_FRAMEWORK
// Target Devices: t35f324
// Tool Versions: EFINITY21.2
// Description: set cmos control signed inout 
// Dependencies: 
// 
// Revision:rev1
// 
// Additional Comments:
// 
//--------------------------------------------------------------------------------

module CmosCtrl_Switch(

input	wire			clk					  ,//clk25m
input	wire	[7:0]	CmosCtrlMode		  ,//select cmos control signals inout and mode    
input	wire			i2c_xclr			  ,//all  
input	wire			XtrigLast			  ,//sony
input	wire			xhs_out				  ,//sony
input	wire			xvs_out				  ,//sony sc2210
input	wire			AmpvLast			  ,//sony
input	wire			Tecen				  ,//not use 
                                              
                                              
output	reg				ScLref				  ,//SC
output	reg				ScFsync				  ,//SC


input  wire 			CMOS_CTL1_IN		  ,//SC2210:PWDNB input power down actice low 
input  wire 			CMOS_CTL2_IN		  ,//SC2210:SD1 
input  wire				CMOS_CTL3_IN		  ,//SC2210:SD0
input  wire				CMOS_CTL4_IN		  ,
input  wire				CMOS_CTL5_IN		  ,
input  wire 			CMOS_XHS_IN			  ,//input XHS
input  wire 			CMOS_XVS_IN			  ,//input XVS 
input  wire				CMOS_AMPV_IN		  , 	
input  wire				CMOS_TECEN_IN		  ,	
input  wire				CMOS_XCLR_IN		  ,
input  wire				CMOS_XMASTER_IN		  ,
input  wire				CMOS_XTRIG_IN		  ,
  		
output reg  			CMOS_TECEN_OUT		  ,			
output reg  			CMOS_TECEN_OE		  ,
output reg  			CMOS_XTRIG_OUT		  ,//SC2210:EFSYNC trig   input 
output reg  			CMOS_XTRIG_OE		  ,
output reg  			CMOS_XMASTER_OUT	  ,//output CMOS XMASTER 		
output reg  			CMOS_XMASTER_OE		  ,
output reg  			CMOS_AMPV_OUT		  ,//output AMPV
output reg  			CMOS_AMPV_OE		  ,
output reg  			CMOS_XCLR_OUT		  ,//output XCLR 	SC2210 :Active low 	input 
output reg  			CMOS_XCLR_OE		  ,
output reg 				CMOS_XHS_OUT		  ,//SC2210:LREF DVP line sync output TO FPGA
output reg 				CMOS_XHS_OE			  ,
output reg 				CMOS_XVS_OUT		  ,//SC2210:FSYNC DVP frame sync output TO FPGA
output reg 				CMOS_XVS_OE			  ,
output reg 				CMOS_CTL1_OUT		  ,
output reg 				CMOS_CTL1_OE		  ,
output reg 				CMOS_CTL2_OUT		  ,
output reg 				CMOS_CTL2_OE		  ,
output reg 				CMOS_CTL3_OUT		  ,
output reg 				CMOS_CTL3_OE		  ,
output reg 				CMOS_CTL4_OUT		  ,
output reg 				CMOS_CTL4_OE		  ,
output reg 				CMOS_CTL5_OUT		  ,
output reg 				CMOS_CTL5_OE		  	


);




always @ (posedge clk )begin 
	case(CmosCtrlMode[2:0])
	1:begin//sony slave mode
				CMOS_CTL1_OE 	<= 1'b0; 	CMOS_CTL1_OUT 		<= 1'b0;//sony not use set input 
				CMOS_CTL2_OE 	<= 1'b0; 	CMOS_CTL2_OUT 		<= 1'b0;//sony not use set input 
				CMOS_CTL3_OE 	<= 1'b0; 	CMOS_CTL3_OUT 		<= 1'b0;//sony not use set input 
				CMOS_CTL4_OE 	<= 1'b0; 	CMOS_CTL4_OUT 		<= 1'b0;//sony not use set input
				CMOS_CTL5_OE 	<= 1'b0; 	CMOS_CTL5_OUT 		<= 1'b0;//sony not use set input 
				CMOS_XHS_OE  	<= 1'b1; 	CMOS_XHS_OUT  		<= xhs_out;//sony output xhs
				CMOS_XVS_OE  	<= 1'b1; 	CMOS_XVS_OUT  		<= xvs_out;//sony output xvs 
				CMOS_AMPV_OE 	<= 1'b1; 	CMOS_AMPV_OUT 		<= AmpvLast;//sony output ampv  
				CMOS_TECEN_OE	<= 1'b0; 	CMOS_TECEN_OUT		<= 1'b0;//sony not use set input 
				CMOS_XCLR_OE 	<= 1'b1; 	CMOS_XCLR_OUT 		<= i2c_xclr; //sony resetn  output active low 
				CMOS_XMASTER_OE <= 1'b1; 	CMOS_XMASTER_OUT 	<= 1'b1;//sony  output set 1 is slave mode 
				CMOS_XTRIG_OE 	<= 1'b1; 	CMOS_XTRIG_OUT 		<= XtrigLast;//sony xtrig 
	end 
	2:begin//sony master  mode
				CMOS_CTL1_OE 	<= 1'b0; 	CMOS_CTL1_OUT 		<= 1'b0;//sony not use set input 
				CMOS_CTL2_OE 	<= 1'b0; 	CMOS_CTL2_OUT 		<= 1'b0;//sony not use set input 
				CMOS_CTL3_OE 	<= 1'b0; 	CMOS_CTL3_OUT 		<= 1'b0;//sony not use set input 
				CMOS_CTL4_OE 	<= 1'b0; 	CMOS_CTL4_OUT 		<= 1'b0;//sony not use set input
				CMOS_CTL5_OE 	<= 1'b0; 	CMOS_CTL5_OUT 		<= 1'b0;//sony not use set input 
				CMOS_XHS_OE  	<= 1'b0; 	CMOS_XHS_OUT  		<= 1'b0;;//sony input xhs
				CMOS_XVS_OE  	<= 1'b0; 	CMOS_XVS_OUT  		<= 1'b0;;//sony input xvs 
				CMOS_AMPV_OE 	<= 1'b1; 	CMOS_AMPV_OUT 		<= AmpvLast;//sony output ampv  
				CMOS_TECEN_OE	<= 1'b0; 	CMOS_TECEN_OUT		<= 1'b0;//sony not use set input 
				CMOS_XCLR_OE 	<= 1'b1; 	CMOS_XCLR_OUT 		<= i2c_xclr; //sony resetn  output active low 
				CMOS_XMASTER_OE <= 1'b1; 	CMOS_XMASTER_OUT 	<= 1'b0;//sony  output set 0 is master mode 
				CMOS_XTRIG_OE 	<= 1'b1; 	CMOS_XTRIG_OUT 		<= XtrigLast;//sony xtrig 
	end 
	3: begin //SC2210	
				CMOS_CTL1_OE 	<= 1'b1; 	CMOS_CTL1_OUT 		<= 1'b1;//SC2210:PWDNB output power down actice low
				CMOS_CTL2_OE 	<= 1'b1; 	CMOS_CTL2_OUT 		<= 1'b0;//SC2210:SD1 output
				CMOS_CTL3_OE 	<= 1'b1; 	CMOS_CTL3_OUT 		<= 1'b0;//SC2210:SD0 output
				CMOS_CTL4_OE 	<= 1'b0; 	CMOS_CTL4_OUT 		<= 1'b0;//sc2210 not use set input
				CMOS_CTL5_OE 	<= 1'b0; 	CMOS_CTL5_OUT 		<= 1'b0;//sc2210 not use set input 
				CMOS_XHS_OE  	<= 1'b0; 	CMOS_XHS_OUT  		<= 1'b0;//sc2210 LREF Line sync input 
				CMOS_XVS_OE  	<= 1'b0; 	CMOS_XVS_OUT  		<= 1'b0;//sc2210 fsync frame sync input 
				CMOS_AMPV_OE 	<= 1'b0; 	CMOS_AMPV_OUT 		<= 1'b0;//sc2210 not use set input 
				CMOS_TECEN_OE	<= 1'b0; 	CMOS_TECEN_OUT		<= 1'b0;//sc2210 not use set input 
				CMOS_XCLR_OE 	<= 1'b1; 	CMOS_XCLR_OUT 		<= i2c_xclr; //sc2210 resetn  output active low 
				CMOS_XMASTER_OE <= 1'b0; 	CMOS_XMASTER_OUT 	<= 1'b0;//sc2210 not user ;set input 
				CMOS_XTRIG_OE 	<= 1'b1; 	CMOS_XTRIG_OUT 		<= ~xvs_out;//XtrigLast;//sc2210 EFSYNC output
		end 		
				
		
	
	default :begin 	
				CMOS_CTL1_OE 	<= 1'b0; 	CMOS_CTL1_OUT 		<= 1'b0;//default set input 
				CMOS_CTL2_OE 	<= 1'b0; 	CMOS_CTL2_OUT 		<= 1'b0;//default set input 
				CMOS_CTL3_OE 	<= 1'b0; 	CMOS_CTL3_OUT 		<= 1'b0;//default set input 
				CMOS_CTL4_OE 	<= 1'b0; 	CMOS_CTL4_OUT 		<= 1'b0;//default set input 
				CMOS_CTL5_OE 	<= 1'b0; 	CMOS_CTL5_OUT 		<= 1'b0;//default set input 
				CMOS_XHS_OE  	<= 1'b0; 	CMOS_XHS_OUT  		<= 1'b0;//default set input 
				CMOS_XVS_OE  	<= 1'b0; 	CMOS_XVS_OUT  		<= 1'b0;//default set input 
				CMOS_AMPV_OE 	<= 1'b0; 	CMOS_AMPV_OUT 		<= 1'b0;//default set input 
				CMOS_TECEN_OE	<= 1'b0; 	CMOS_TECEN_OUT		<= 1'b0;//default set input 
				CMOS_XCLR_OE 	<= 1'b0; 	CMOS_XCLR_OUT 		<= 1'b0;//default set input 
				CMOS_XMASTER_OE <= 1'b0; 	CMOS_XMASTER_OUT 	<= 1'b0;//default set input 
				CMOS_XTRIG_OE 	<= 1'b0; 	CMOS_XTRIG_OUT 		<= 1'b0;//default set input 
		end 		
endcase 
end          
          
          
always @ (posedge clk )begin 
	case( CmosCtrlMode[2:0])
	3:begin //SC2210
			ScLref<=CMOS_XHS_IN;
			ScFsync<=CMOS_XVS_IN;
		
		end 
	default :begin
			ScLref<=0;
			ScFsync<=0;
		
		
	end 
	endcase  
end 
endmodule