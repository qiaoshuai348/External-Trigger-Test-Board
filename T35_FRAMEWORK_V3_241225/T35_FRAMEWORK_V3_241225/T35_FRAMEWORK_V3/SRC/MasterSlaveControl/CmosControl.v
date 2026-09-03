`timescale 1ns / 1ps
//-------------------------------------------------------------------------------
// Company:  QHYCCD
// Engineer: YangSK
// 
// Create Date: 2022/6/6
// Design Name: T35_TOP
// Module Name: T35_TOP
// Project Name: T35_FRAMEWORK
// Target Devices: t35f324
// Tool Versions: EFINITY21.2
// Description: set master mode or slave mode
// Dependencies: 
// 
// Revision:rev1
// 
// Additional Comments:
// 
//--------------------------------------------------------------------------------

module CmosControl(

	input		wire			clk					,
	input		wire			rst					,
	input		wire			IDLE				,
	//input		wire			SingleFrameCapture	,		
	input		wire			i2c_xclr			,	
	input		wire			trigin_gpio			, //gpio trig in
	input   	wire			trigin_optic		, //optic trig in active low ,It is usually high level	
	input		wire [23:0]		FilterTime		    , // in this mode(mode2), xTrigExpTime Determines the interval between IDLE  and RELEASEIDLE  xTrigExpTime value*40ns == xx ns 25000 000 ;
	input		wire [7:0]		test_mode			, //reg39 gpio trig enable 
	input		wire [7:0]		TrigMode    		, //reg58 Master switch,enable trigout 
	input		wire [7:0]		TrigModeA   		,//TrigModeA   reg153
	input		wire			ampv_enable			,
	input   	wire			AMPV_MANUAL			,//reg49
	input   	wire [7:0]		AMPV_MODE			,//reg143				
	input   	wire [31:0] 	VMAX				,
	input   	wire [31:0] 	HMAX				,					
	input   	wire [31:0] 	AMPV_START			,
	input   	wire [31:0] 	AMPV_END			,	
	input   	wire [15:0] 	VMAX_2_LSB			,	
	input   	wire [7:0] 		burst_start 		,
	input   	wire [15:0] 	burst_end 			,
	input   	wire	    	EnableBurstMode 	,
	input 		wire [31:0]		TFPP_time 			,// trig fall prohibited period
	input 		wire [31:0]		TRPP_time			,// trig rise prohibited period																															                                         	
	input		wire			Xtrig_rstn			,
	input 		wire			LoopExp				,
	input   	wire [39:0] 	ExpTime				,
	input 		wire			isFrameEndlong		,
	        	
	input   	wire [15:0] 	hsync_stled			,//  GPIO mode6 led
	input   	wire [15:0] 	hsync_edled 		,//  GPIO mode6 led
	input   	wire 			m6inline_leden		,
	input   	wire [15:0] 	m6inline_st  		,
	input   	wire [15:0] 	m6inline_ed			, 
  	      
//cmos 12pin +1 inck pin   
	input		wire [7:0]		CmosCtrlMode		,
	input  		wire 			CMOS_CTL1_IN		, 
  	input  		wire 			CMOS_CTL2_IN		,
  	input  		wire			CMOS_CTL3_IN		,
  	input  		wire			CMOS_CTL4_IN		,
  	input  		wire			CMOS_CTL5_IN		,
  	input  		wire 			CMOS_XHS_IN			,
  	input  		wire 			CMOS_XVS_IN			, 
  	input  		wire			CMOS_AMPV_IN		, 	
  	input  		wire			CMOS_TECEN_IN		,	
  	input  		wire			CMOS_XCLR_IN		,
  	input  		wire			CMOS_XMASTER_IN		,
 	input  		wire			CMOS_XTRIG_IN		,
  		
  	output 		wire 			CMOS_TECEN_OUT		,			
  	output 		wire 			CMOS_TECEN_OE		,
  	output 		wire 			CMOS_XTRIG_OUT		,
	output 		wire 			CMOS_XTRIG_OE		,
	output 		wire 			CMOS_XMASTER_OUT	,
  	output 		wire 			CMOS_XMASTER_OE		,
  	output 		wire 			CMOS_AMPV_OUT		,
  	output 		wire 			CMOS_AMPV_OE		,
  	output 		wire 			CMOS_XCLR_OUT		,
  	output 		wire 			CMOS_XCLR_OE		,
  	output 		wire			CMOS_XHS_OUT		,
  	output 		wire			CMOS_XHS_OE			,
  	output 		wire			CMOS_XVS_OUT		,
  	output 		wire			CMOS_XVS_OE			,
  	output 		wire			CMOS_CTL1_OUT		,
  	output 		wire			CMOS_CTL1_OE		,
  	output 		wire			CMOS_CTL2_OUT		,
  	output 		wire			CMOS_CTL2_OE		,
  	output 		wire			CMOS_CTL3_OUT		,
  	output 		wire			CMOS_CTL3_OE		,
  	output 		wire			CMOS_CTL4_OUT		,
  	output 		wire			CMOS_CTL4_OE		,
  	output 		wire			CMOS_CTL5_OUT		,
  	output 		wire			CMOS_CTL5_OE		,
	                                     	
	output  	wire [39:0] 	expCounter			,	
	output		wire			trigout_gpio		,
	output		wire			trigout_optic		,		
	output  	wire			trigin_or_idle  	,
	output  	wire   			mode6_led 				
					                                 	
);

	wire		trigout_slave	;
	wire		trigout_xtrig	;
	wire		trigin_or_idle	;
	wire 		XtrigLast		;
	wire		xhs_in			;
	wire		xhs_in			;
	wire		AmpvLast		;
	wire 		ScLref			;
	wire		ScFsync			;


 SlaveControl_2022 	SlaveControl_2022_inst(

		.clk_in				(clk			),// 
		.IDLE_ctrl			(trigin_or_idle	), 
		.ampv_enable		(ampv_enable	),	
		.AMPV_MANUAL		(AMPV_MANUAL	),//7:0
		.AMPV_MODE		    (AMPV_MODE		),//7:0								
		.VMAX				(VMAX			),
		.HMAX				(HMAX			),					
		.AMPV_START			(AMPV_START		),
		.AMPV_END			(AMPV_END		),																							
							                    																
		.VMAX_2_LSB			(VMAX_2_LSB		),					
		.burst_start 		(burst_start	),
		.burst_end 			(burst_end		),
		.EnableBurstMode 	(EnableBurstMode),	

//gpio mode 6 ,led ,		                  
		.hsync_stled		(hsync_stled	),//  GPIO mode6 led
		.hsync_edled 		(hsync_edled	),//  GPIO mode6 led
		.m6inline_leden		(m6inline_leden	),
		.m6inline_st  		(m6inline_st	),
		.m6inline_ed		(m6inline_ed	),
		.mode6_led 			(mode6_led		),
		
		.ampv_out			(AmpvLast		),//output                   
		.trigout_slave 	    (trigout_slave	),//output
		.xhs_out 			(xhs_out		),//output
		.xvs_out 			(xvs_out		),//output	
		
//********xTrigExposure*************************
					
        .Xtrig_rstn			(Xtrig_rstn		),					
        .LoopExp			(LoopExp		), 						
        .ExpTime			(ExpTime		),//unit 40ns			
        .isFrameEndlong		(isFrameEndlong	),
		.TFPP_time 			(TFPP_time		),
		.TRPP_time			(TRPP_time		),
      																
        .XTRIG				(XtrigLast		),
        .Xtrig_out			(trigout_xtrig	),		
        .expCounter			(expCounter		),
					
//************masterModeAMPV********************
					
        .xhs_in				(xhs_in			),
        .xvs_in				(xvs_in			)
						
);
//MasterSlaveControl


 Trigout_set	Trigout_set_inst(

		.clk				(clk			),//25mhz
		.TrigMode 			(TrigMode		),//reg58 Master switch,enable trigout 
		.TrigModeA		    (TrigModeA		),//TrigModeA   reg153
		.trigout_slave		(trigout_slave	),  
		.trigout_xtrig		(trigout_xtrig	),
		.trigin				(trigin_or_idle	),
		                
		.trigout_gpio		(trigout_gpio	),
		.trigout_optic		(trigout_optic	)	

);
//Trigout_set


 Trigin_set	Trigin_set_inst(

		.clk				(clk				), //25M  40NS
		.IDLE				(IDLE				),
		//.SingleFrameCapture	(SingleFrameCapture	),
		.test_mode			(test_mode			), //reg39 gpio trig enable 
		.TrigMode    		(TrigMode			), //reg58 Master switch,enable trigout 
		.TrigModeA   		(TrigModeA			),//TrigModeA   reg158		                                          
		.trigin_gpio		(trigin_gpio		), //gpio trig in	
		.FilterTime		    (FilterTime			), // in this mode(mode2), xTrigExpTime Determines the interval between IDLE  and RELEASEIDLE  xTrigExpTime value*40ns == xx ns 25000 000 ;
		.trigin_optic		(trigin_optic		), //optic trig in ，active low ,It is usually high level
		                 
		.trigin_or_idle     (trigin_or_idle		)
		
);
//Trigin_set



 CmosCtrl_Switch CmosCtrl_Switch_inst(

	.clk				(clk				),//clk25m
	.CmosCtrlMode		(CmosCtrlMode		),//select cmos control signals inout and mode    
	.i2c_xclr			(i2c_xclr			),//all  
	.XtrigLast			(XtrigLast			),//sony
	.xhs_out 			(xhs_out			),//output
	.xvs_out 			(xvs_out			),//output	
	.AmpvLast			(AmpvLast			),//sony
	.Tecen				(AMPV_MANUAL		),//not use 
//output                                                                                   
	.ScLref				(ScLref				),//SC
	.ScFsync			(ScFsync			),//SC
                                           
// FPGA GPIO                                            
	.CMOS_CTL1_IN		(CMOS_CTL1_IN	    ),//SC2210:PWDNB input power down actice low 
	.CMOS_CTL2_IN		(CMOS_CTL2_IN	    ),//SC2210:SD1 
	.CMOS_CTL3_IN		(CMOS_CTL3_IN	    ),//SC2210:SD0
	.CMOS_CTL4_IN		(CMOS_CTL4_IN	    ),
	.CMOS_CTL5_IN		(CMOS_CTL5_IN	    ),
	.CMOS_XHS_IN		(CMOS_XHS_IN		),//input XHS
	.CMOS_XVS_IN		(CMOS_XVS_IN		),//input XVS 
	.CMOS_AMPV_IN		(CMOS_AMPV_IN	    ), 	
	.CMOS_TECEN_IN		(CMOS_TECEN_IN	    ),	
	.CMOS_XCLR_IN		(CMOS_XCLR_IN	    ),
	.CMOS_XMASTER_IN	(CMOS_XMASTER_IN	),
	.CMOS_XTRIG_IN		(CMOS_XTRIG_IN	    ),
//output  		                                   
	.CMOS_TECEN_OUT		(CMOS_TECEN_OUT	    ),			
	.CMOS_TECEN_OE		(CMOS_TECEN_OE	    ),
	.CMOS_XTRIG_OUT		(CMOS_XTRIG_OUT	    ),//SC2210:EFSYNC trig   input 
	.CMOS_XTRIG_OE		(CMOS_XTRIG_OE	    ),
	.CMOS_XMASTER_OUT	(CMOS_XMASTER_OUT   ),//output CMOS XMASTER 		
	.CMOS_XMASTER_OE	(CMOS_XMASTER_OE	),
	.CMOS_AMPV_OUT		(CMOS_AMPV_OUT	    ),//output AMPV
	.CMOS_AMPV_OE		(CMOS_AMPV_OE	    ),
	.CMOS_XCLR_OUT		(CMOS_XCLR_OUT	    ),//output XCLR 	SC2210 :Active low 	input 
	.CMOS_XCLR_OE		(CMOS_XCLR_OE	    ),
	.CMOS_XHS_OUT		(CMOS_XHS_OUT	    ),//SC2210:LREF DVP line sync output TO FPGA
	.CMOS_XHS_OE		(CMOS_XHS_OE		),
	.CMOS_XVS_OUT		(CMOS_XVS_OUT	    ),//SC2210:FSYNC DVP frame sync output TO FPGA
	.CMOS_XVS_OE		(CMOS_XVS_OE		),
	.CMOS_CTL1_OUT		(CMOS_CTL1_OUT	    ),
	.CMOS_CTL1_OE		(CMOS_CTL1_OE	    ),
	.CMOS_CTL2_OUT		(CMOS_CTL2_OUT	    ),
	.CMOS_CTL2_OE		(CMOS_CTL2_OE	    ),
	.CMOS_CTL3_OUT		(CMOS_CTL3_OUT	    ),
	.CMOS_CTL3_OE		(CMOS_CTL3_OE	    ),
	.CMOS_CTL4_OUT		(CMOS_CTL4_OUT	    ),
	.CMOS_CTL4_OE		(CMOS_CTL4_OE	    ),
	.CMOS_CTL5_OUT		(CMOS_CTL5_OUT	    ),
	.CMOS_CTL5_OE		(CMOS_CTL5_OE	    )

);

endmodule