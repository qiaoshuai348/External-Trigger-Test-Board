`timescale 1ns / 1ps
//-------------------------------------------------------------------------------
// Company:  QHYCCD
// Engineer: YangSK
// Create Date: 2022/4/6
// Design Name: T35_TOP
// Module Name: T35_TOP
// Project Name: T35_FRAMEWORK
// Target Devices: t35f324
// Tool Versions: EFINITY21.2
// Dependencies: 
// Revision:T35PCBV2   		
// 2022 10 28 :
// Change i2c  registers 18, 19, 20, 21 corresponding to R G1, B G2, respectively
// 2023 07 19 :
//  1 gain GAIN Takes effect at NEST frame
//  2 Modified the burst  mode packet patch mechanism 
//  3 Modify the XTRIG mode state machine
//  4 Modify the trig in selection mechanism
//  5 Added MIPI raw 8 data parsing function
//2023 07 25:
// Added code to output MIPI IP virtual channel 1, 2, 3 data
//2023 08 03 
// Add horizontal pixel padding.
//2023 10 27 V3.1
// Modify the initialization status of the I2 SLAVE module
//2023 11 11 v3.2
//Modified the bug that the gain is abnormal in MIPI RAW8 mode

 `define      RDFIFI_MAX 		     512 //unit 128bit                   
 `define      DDR3ADDRE_MAXBIT     	 28                   
 `define      ALEN_0		     	 8  //test is 8 ,normal is 64              
 `define      STOP_ADDR_0	     	 32'h10000000  //test is 16384 ,normal is  32'h10000000,check done address    Reducing this parameter speeds up ddr initialization   
 `define      START_ADDR_0           32'H0	              
 `define 	  DDR3ADDRE_MAX			 32'h1FFFFFFF  //test is 16384 ,normal is  32'h1FFFFFFF,ddr3 max address ,E.P.: AXI ADDRESS is [28:0] =   32'h1FFFFFFF 
 `define 	  COUNT_ENDBIT 			 12     // TEST is8 ,normal is 12

 //`define      DDRTEST_BW						// test DDR band width in mpdule ddr3_state.v  
 //`define 	    FX3TXNUMTEST					// test how many data have send to fx3  in module ddrorbypass.v    
 //`define      DDRTEST_DATA					// Generate incremental data to test  DDR book     
 //`define 		FX3TESTDATA_Gen   //TEST Fx3 data generate
 //`define   	CloseWatchDog//in debug mode ,set fx3 reset always is 1; 
 //`define 		MIPIDATA_TEST 
module T35_TOP(

//****************user interface*************************************************** 
//clock
		input	wire			sysclk				,//50M	
		input 	wire			ddrrefclk			,//100M	            		
		input 	wire 			I2CSCL_0			,     
		input  wire				USB_RESET			,//pull dowm NOT USE //minicam8 not use      		
		input  wire				optic_trigin		,//pullup //minicam8 not use

//gpio		                                    		
		input wire				MUX_GPIO1_IN		,//minicam8 not use
  		input wire				MUX_GPIO2_IN		,//minicam8 not use
  		input wire				MUX_GPIO3_IN		,//minicam8 not use
  		input wire				MUX_GPIO4_IN		,//minicam8 not use		            			
     	input wire				TP_GPIO5_IN			,
  		input wire				TP_GPIO6_IN			,
  		input wire				TP_GPIO7_IN			,
  		input wire				TP_GPIO8_IN			,
  		
    	output wire				MUX_GPIO1_OUT		,//minicam8 not use
  		output wire				MUX_GPIO1_OE		,//minicam8 not use
  		output wire				MUX_GPIO2_OUT		,//minicam8 not use
  		output wire				MUX_GPIO2_OE		,//minicam8 not use
  		output wire				MUX_GPIO3_OUT		,//minicam8 not use
  		output wire				MUX_GPIO3_OE		,//minicam8 not use
  		output wire				MUX_GPIO4_OUT		,//minicam8 not use
  		output wire				MUX_GPIO4_OE		,//minicam8 not use	
   		output wire				TP_GPIO5_OUT		,
  		output wire				TP_GPIO5_OE			,	
   		output wire				TP_GPIO6_OUT		,//not use 
  		output wire				TP_GPIO6_OE			,
  		output wire				TP_GPIO7_OUT		,
  		output wire				TP_GPIO7_OE			,
  		output wire				TP_GPIO8_OUT		,
  		output wire				TP_GPIO8_OE			,
  		output wire				optic_trigout		,//minicam8 not use
  		
//LED 
		output	wire			LED_R				,
		output	wire			LED_G				,
		output	wire			LED_B				,
//UART	
		input	wire			UART_FPGARX		  	,//minicam8 not use
		input	wire			UART_FX3TX			,	
		output	wire			UART_FX3RX			,//UART_FX3RX=UART_FPGARX		
		output	wire			UART_FPGATX			,//UART_FPGATX=UART_FX3TX	 //minicam8 not use
		
//fx3 		    	              		
		output	wire			fx3_resetn			,	
		input	wire			fx3_ctl1			,//fx3_rdy				
		input	wire			fx3_ctl6			,//fx3 sdo ,fx3 gpio 23  
		input	wire			fx3_ctl4			,//fx3 sck
		input	wire			fx3_ctl5			,//fx3 sdi 
		input	wire			fx3_ctl7			,//fx3 xce 
		output	wire			fx3_ctl12			,//fx3_slwr  A0
		output	wire			fx3_ctl11			,//fx3_pkend A1
		output	wire [31:0]		fx3_data			,

//cmos 12pin +1 inck pin
		input  wire 			CMOS_CTL1_IN		,//SC2210:PWDNB input power down actice low 
  		input  wire 			CMOS_CTL2_IN		,//SC2210:SD1 
  		input  wire				CMOS_CTL3_IN		,//SC2210:SD0
  		input  wire				CMOS_CTL4_IN		,////minicam8 not use
  		input  wire				CMOS_CTL5_IN		,//minicam8 not use
  		input  wire 			CMOS_XHS_IN			,//input XHS
  		input  wire 			CMOS_XVS_IN			,//input XVS 
  		input  wire				CMOS_AMPV_IN		,//minicam8 not use	
  		input  wire				CMOS_TECEN_IN		,	
  		input  wire				CMOS_XCLR_IN		,
  		input  wire				CMOS_XMASTER_IN		,
 		input  wire				CMOS_XTRIG_IN		,
  		
  		output wire 			CMOS_TECEN_OUT		,			
  		output wire 			CMOS_TECEN_OE		,
  		output wire 			CMOS_XTRIG_OUT		,//SC2210:EFSYNC trig   input 
		output wire 			CMOS_XTRIG_OE		,
		output wire 			CMOS_XMASTER_OUT	,//output CMOS XMASTER 		
  	    output wire 			CMOS_XMASTER_OE		,
  	    output wire 			CMOS_AMPV_OUT		,//output AMPV //minicam8 not use
  	    output wire 			CMOS_AMPV_OE		,//minicam8 not use
  		output wire 			CMOS_XCLR_OUT		,//output XCLR 	SC2210 :Active low 	input 
  		output wire 			CMOS_XCLR_OE		,
  		output wire				CMOS_XHS_OUT		,//SC2210:LREF DVP line sync output TO FPGA
  		output wire				CMOS_XHS_OE			,
  		output wire				CMOS_XVS_OUT		,//SC2210:FSYNC DVP frame sync output TO FPGA
  		output wire				CMOS_XVS_OE			,
  		output wire				CMOS_CTL1_OUT		,
  		output wire				CMOS_CTL1_OE		,
  		output wire				CMOS_CTL2_OUT		,
  		output wire				CMOS_CTL2_OE		,
  		output wire				CMOS_CTL3_OUT		,
  		output wire				CMOS_CTL3_OE		,
  		output wire				CMOS_CTL4_OUT		,
  		output wire				CMOS_CTL4_OE		,
  		output wire				CMOS_CTL5_OUT		,
  		output wire				CMOS_CTL5_OE		,
  		
		//output	wire			fx3_pclk			,   //PLL OUTPUT 

//reconfigure 
		input 					cfg_ERROR			,
		output [1:0] 			cfg_CBSEL			,//
  		output 					cfg_CONFIG			,
  		output 					cfg_ENA				,
//FLASH SPI 	 		  
 	    input   wire			miso 				,	 		  
 		//input   wire			miso_1 		        ,
 		//input   wire			miso_2 		        ,
 		//input   wire			miso_3 		        ,
 		                                       
 		output  wire 			sclk 			 	,	  
 		output  wire 			CS0 			 	,
 		output	wire			CS1					, //minicam8 not use  
 		//output  wire 			mosi 			 	,	
 		output 					mosi_OUT			,
  		output 					mosi_OE				,
 		
 		output  wire 			HOLD_N				,
  		output  wire			WP_N				,  
 		//output  wire 			mosi_1 		     	,	  
 		//output  wire 			mosi_2 		     	,	  
 		//output  wire 			mosi_3 		     	,	  
 		//output  wire 			mosi_oe 		 	,	
 		//output  wire			mosi_oe1			, 
 		//output  wire			mosi_oe2			, 
 		//output  wire			mosi_oe3			,   		    		    		    		  
//I2C SDA 
	  	input 	wire    		I2CSDA_0_IN			,
   		output  wire 			I2CSDA_0_OUT		,
  		output  wire 			I2CSDA_0_OE			,//inout I2CSDA_0
//PLL 			        		
 		input 	wire 			syspll_LOCKED		,
  		input 	wire 			ddrpll_LOCKED		,
  		input 	wire			CMOSPLL_LOCKED		,
   		input 	wire 			syspll_CLKOUT25		,
  		input 	wire 			syspll_CLKOUT100	,  		    
     	input 	wire			CMOSPLL_CMOSCLKOUT	,//CMOS INCLK 
     	//input 	wire			ddrpll_CLKOUT8Mhz	,//ddrpll_CLKOUT8Mhz 
//MIPI 
  // MIPI Video input
  		input 	wire [3:0] 		mipi_rx_inst1_CNT	,
  		input 	wire [63:0] 	mipi_rx_inst1_DATA	,
  		input 	wire [17:0] 	mipi_rx_inst1_ERROR	,
  		input 	wire [3:0] 		mipi_rx_inst1_HSYNC	,
  		input 	wire [5:0] 		mipi_rx_inst1_TYPE	,
  		input 	wire [3:0] 		mipi_rx_inst1_ULPS	,
  		input 	wire			mipi_rx_inst1_ULPS_CLK,
  		input 	wire			mipi_rx_inst1_VALID	,
  		input 	wire [1:0] 		mipi_rx_inst1_VC	,
  		input 	wire [3:0] 		mipi_rx_inst1_VSYNC	,
 
  // MIPI Control
  		output  wire  			mipi_rx_inst1_CLEAR	,//reset MIPI error register
  		output  wire 			mipi_rx_inst1_DPHY_RSTN,
  		output  wire [1:0] 		mipi_rx_inst1_LANES	,//2'B11
  		output  wire 			mipi_rx_inst1_RSTN	,
  		output  wire [3:0] 		mipi_rx_inst1_VC_ENA,
//DDR
		//input 	wire			DDR_CTRL_CFG_SDA_OEN,
 		//output 	wire			DDR_CTRL_CFG_SCL_IN	,
  		//output 	wire			DDR_CTRL_CFG_SDA_IN	,
  		
  		output 	wire			DDR_CTRL_CFG_SEQ_RST,
  		output  wire			DDR_CTRL_CFG_SEQ_START,
    	output 	wire			DDR_CTRL_CFG_RST_N	,
	
  		input 					DDR_CTRL_AREADY_0	,  //**Address ready.
  		input [7:0] 			DDR_CTRL_BID_0		,  //Response ID tag. This signal is the ID tag of the write response.
  		input 					DDR_CTRL_BVALID_0	,  //Write response valid. This signal indicates that the channel is signaling a valid write response.                  
  		input [127:0] 			DDR_CTRL_RDATA_0	,  //**Read data.              
  		input [7:0] 			DDR_CTRL_RID_0		,  //Read ID tag. This signal is the identification tag for the read data group of signals generated by the slave.        
  		input 					DDR_CTRL_RLAST_0	,  //**Read last. This signal indicates the last transfer in a read burst.                      
  		input [1:0] 			DDR_CTRL_RRESP_0	,  //Read response. This signal indicates the status of the read transfer
  		input 					DDR_CTRL_RVALID_0	,  //**Read valid
  		input 					DDR_CTRL_WREADY_0	,  //**Write ready. This signal indicates that the slave can accept the write data.
  		                		                                                                                          
  		output [31:0] 			DDR_CTRL_AADDR_0	,  //** Address. ATYPE defines whether it is a read or write address. It gives the address of the first transfer in a burst transaction. 
  		output [1:0] 			DDR_CTRL_ABURST_0	,  // Burst type. The burst type and the size determine how the address  for each transfer within the burst is calculated.               
  		output [7:0] 			DDR_CTRL_AID_0		,  // Address ID. This signal identifies the group of address signals. Depends on ATYPE, the ID can be for a read or write address group             
  		output [7:0] 			DDR_CTRL_ALEN_0		,  // Burst length. This signal indicates the number of transfers in a burst
  		output [1:0] 			DDR_CTRL_ALOCK_0	,  // Lock type. This signal provides additional information about the  atomic characteristics of the transfer  
  		output [2:0] 			DDR_CTRL_ASIZE_0	,  // Burst size. This signal indicates the size of each transfer in the burst.                       
  		output 					DDR_CTRL_ATYPE_0	,  //** This signal distinguishes whether is it is a read or write operation. 0= read and 1 = write.
  		output 					DDR_CTRL_AVALID_0	,  //** Address valid. This signal indicates that the channel is signaling valid address and control information.                                                
  		output 					DDR_CTRL_BREADY_0	,  // Response ready. This signal indicates that the master can accept a write response                            
  		output 					DDR_CTRL_RREADY_0	,  //** Read ready. This signal indicates that the master can accept the read data and response information.                                              
  		output [127:0] 			DDR_CTRL_WDATA_0	,  //** Write data.
  		output [7:0] 			DDR_CTRL_WID_0		,  // Write ID tag. This signal is the ID tag of the write data transfer
  		output 					DDR_CTRL_WLAST_0	,  //** Write last. This signal indicates the last transfer in a write burst
  		output [15:0] 			DDR_CTRL_WSTRB_0	,  // Write strobes. This signal indicates which byte lanes hold valid data. There is one write strobe bit for each eight bits of the write data bus  
  		output 					DDR_CTRL_WVALID_0      //** Write valid. This signal indicates that valid write data and strobes are  available   
	                                 

);

localparam REMA_NUM_MAXBIT   =    `DDR3ADDRE_MAXBIT - 4;  
  

wire			i2c_xclr    		;   
wire			trigin_gpio			;  
wire [23:0]		FilterTime			;	
wire [7:0]		test_mode			;
wire [7:0]		TrigMode    		;
wire [7:0]		TrigModeA   		;
wire			ampv_enable			;
                                                                     
wire			AMPV_MANUAL			;                                    
wire [7:0]		AMPV_MODE			;                                    
wire [31:0] 	VMAX				;                                          
wire [31:0] 	HMAX				;                                          
wire [31:0] 	AMPV_START			;                                          
wire [31:0] 	AMPV_END			;                                          
wire [15:0] 	VMAX_2_LSB			;                                                                                                                                                       
wire			Xtrig_rstn			;                                          
wire			LoopExp				;                                          
wire [39:0] 	ExpTime				;   
wire [31:0] 	TFPP_time			;       
wire [31:0] 	TRPP_time			;                                
wire			isFrameEndlong		;     
wire 			trigout_gpio		;	
wire 			trigin_or_idle  	;
wire			framed_long			;  
//others
wire [7:0]		CmosCtrlMode			;
wire				isddr				;
wire				FrameNumEn			;
wire				TrigSignalEn   		;       
wire				guide1_gps_ctl		;   
wire				guide2				; 
wire				guide3				;  
wire				guide4				;   
wire				GPSBOX_DataReceived ;      

wire  [31:0] 		gps1				;
wire  [31:0] 		gps2	            ;
wire  [31:0] 		gps3	            ;
wire  [31:0] 		gps4	            ;
wire  [31:0] 		gps5	            ;
wire  [31:0] 		gps6	            ;
wire  [31:0] 		gps7	            ;
wire  [31:0] 		gps8	            ;
wire  [31:0] 		gps9	            ;

wire				EnableBurstMode		;     
wire  [31:0]		PatchVNumber    	;
wire  [7 :0]  		BurstStart	    	;
wire  [15:0]  		BurstEnd  	    	;


// frame signals 
wire	[65:0]		fx3_wr_data			;
wire				fx3_wr_vaild		;
wire 	[63:0]		mipi_rxdata			;	
wire 				mipi_rxvld			;	
wire	[63:0]		Data64_after1		;
wire				Data64Wr_after1		;
wire				mipi_rxvsyncvld 	;	
wire 				mipi_rxhsyncvld 	;	
wire				mipi_line_st		;
wire				mipi_line_ed		;
wire 				frame_st	 		;
wire 				frame_ed  	    	;

                                    	
//flash ctrl     
wire				asmi_wpen			;
wire				asmi_reset			;//reg115
wire	[1:0]		asmi_flashcs        ;
wire	[7:0]		asmi_num            ;//reg135
wire	[23:0]		asmi_address		;	
wire				asmi_writestart 	;
wire				asmi_readstart	    ;
wire				asmi_erasestart     ;
wire	[7:0]		asmi_didata		    ;
wire				asmi_divld		    ;                	                
wire	[7:0]		asmi_dataout	    ;
wire				asmi_dovalid	    ;
wire				busy 				;                                                                 	                                                      
wire	[1:0]		remote_sel    		; 
wire 				remote_reconfig		;	 

//frame Control signal
wire				IDLE				;   
//wire				SingleFrameCapture	;//reg159  //2023 07 04 take out SingleFrameCapture
wire	[7:0]		gain_18				;	
wire	[7:0]		gain_19		        ;
wire	[7:0]		gain_20		        ;
wire 	[7:0] 		gain_21				;
wire    [7:0] 		gain_mode			;	
wire				is16bit				;

//ddr signals
wire						skip_check		;
wire						flag_full  		;         
wire [7:0] 					ddr_code		;// {DataRdTimeOut_flag��ddr_pfull,ddr_alempty,ddr_alfull,addr_err,check_fail,check_done};                                                                                                                                                             
wire [`DDR3ADDRE_MAXBIT:0] 	threshold_num 	;//default is 64Mbyte
wire [`DDR3ADDRE_MAXBIT:0] 	rema_num  		;
wire [63:0]					rddr3_data		;
wire						rddr3_vld		;
wire						DDR_CLG_R 		;  
reg  						ddr_rstn=0		;  
wire 						regddr_rstn		;

                                       	                                                                               	
//fpga information                      	
wire	[7:0]		year		     		;
wire	[7:0]		month		        	;
wire	[7:0]		day			        	;
wire	[7:0]		subversion1	        	;
wire	[7:0]		subversion2	        	;
wire	[7:0]		boardty             	;

reg 				mipi_rstn =1   			; 
wire				ac_mipi_rstn			;
reg 	[2:0]		init_rstn_cnt	=0		;
wire	[7:0]		DecodeMode				;////test mode register
wire    [15:0]		DetectedBw				;   
wire	[15:0]		DetectedXSize			;     
wire	[15:0]		DetectedYSize			;     
reg 	[3:0]		tp_cnt		=0			;

wire	[7:0]     	fpga_state1				;
wire	[7:0]		fpga_state2				;
wire    [7:0]		fpga_state3				;
wire	[7:0]		fpga_state4				;
wire 	[7:0]		fpga_state_indicate		;
wire	[7:0] 		rstn_num				;
wire				watchdogenable			;
wire				driver_feed_dogs		;
wire				mipierr_bit0_5			;//err_esc;  crc_error_vc0-vc3; hs_rx_timeout_err
wire	[7:0]		LED_control				;

//2023 08 03
wire [7:0] xpatch;
//******************************************************************************************************************************************
assign mipierr_bit0_5 = mipi_rx_inst1_ERROR[0]|mipi_rx_inst1_ERROR[1]|mipi_rx_inst1_ERROR[2]|mipi_rx_inst1_ERROR[3]|mipi_rx_inst1_ERROR[4]|mipi_rx_inst1_ERROR[5];
assign fpga_state1 = {mipierr_bit0_5,mipi_rx_inst1_ERROR[9],cfg_ERROR,ddr_code[4:0]};  //  {mipierr_bit0_5,MIPI Ecc no error,cfg_ERROR,ddr_alempty,ddr_alfull,addr_err,check_fail,init_done_flag};
assign fpga_state2 = mipi_rx_inst1_ERROR[17:10];
assign fpga_state3 = {busy};
assign mipi_rx_inst1_CLEAR = ~IDLE;
//assign CMOSCTL_2 = 1'b0 ;


initial begin  
	init_rstn_cnt <= 0 ;
	//mipi_rx_inst1_CLEAR<=0;
	mipi_rstn<=1;
	ddr_rstn <=1;
end                                        
                                 
always @ (posedge syspll_CLKOUT100)begin 
		if(init_rstn_cnt[2])begin 
			init_rstn_cnt <= init_rstn_cnt ;
		end else begin 
			init_rstn_cnt <= init_rstn_cnt + 1'b1;
		end 
	end 
//init_rstn_cnt

//assign mipi_rstn = init_rstn_cnt[2]&ac_mipi_rstn&frame_ed; 
//assign ddr_rstn  = init_rstn_cnt[2]&regddr_rstn;
//initial  rstn 
always @ (posedge syspll_CLKOUT100)begin
 mipi_rstn = init_rstn_cnt[2]&ac_mipi_rstn;//&(~frame_ed); 
 ddr_rstn  = init_rstn_cnt[2]&regddr_rstn;
end 
//mipi_rstn
//ddr_rstn

always@(posedge syspll_CLKOUT25)begin 
		tp_cnt <= tp_cnt + 1'b1;
end 		
assign TP_GPIO5_OUT = tp_cnt[3];	
assign TP_GPIO5_OE=1;
// TP GPIO WAVEFORM	

assign UART_FX3RX=UART_FPGARX;
assign UART_FPGATX=UART_FX3TX;
//******************************************************************************************************************************************


//****************************test start**************************************************** 

`ifdef	FX3TESTDATA_Gen
wire [63:0] Fx3TestData		;
wire		Fx3TestDVld		;	
 fx3testdata	fx3testdata_inst(
		
		.clk					(syspll_CLKOUT100	),
		.flag_full				(flag_full			),
	          
		.datavld				(Fx3TestDVld		),//Fx3TestDVld
		.data	    			(Fx3TestData		)//Fx3TestData
		
);
`endif

 
                                                                                                                                                     
//****************************test end **************************************************** 


 CmosControl	CmosControl_inst(

	.clk					(syspll_CLKOUT25	),
	.rst					(0					),
	.IDLE					(IDLE				),//reg35
	//.SingleFrameCapture		(SingleFrameCapture	),//reg159  //2023 07 04 take out SingleFrameCapture
	.i2c_xclr				(i2c_xclr			),
	.trigin_gpio			(trigin_gpio		), //gpio trig in
	.trigin_optic			(optic_trigin		), //optic trig in active low ,It is usually high level	
	.FilterTime		    	(FilterTime			), // in this mode(mode2), xTrigExpTime Determines the interval between IDLE  and RELEASEIDLE  xTrigExpTime value*40ns == xx ns 25000 000 ;
	.test_mode				(test_mode			), //reg39 gpio trig enable 
	.TrigMode    			(TrigMode			), //reg58 Master switch,enable trigout 
	.TrigModeA   			(TrigModeA			),//TrigModeA   reg153
	.ampv_enable			(ampv_enable		),//ampv_control reg08
	.AMPV_MANUAL			(AMPV_MANUAL		),//reg49
	.AMPV_MODE				(AMPV_MODE			),//reg143				
	.VMAX					(VMAX				), //reg25-24-23-22
	.HMAX					(HMAX				),	//reg29-28-27-26				
	.AMPV_START				(AMPV_START			),//reg15-14;reg17-16
	.AMPV_END				(AMPV_END			),	//reg10-9;reg13-12
	.VMAX_2_LSB				(VMAX_2_LSB			),	//reg46,-45
	.burst_start 			(BurstStart			),  
	.burst_end 				(BurstEnd			),
	.EnableBurstMode 		(EnableBurstMode	),																															                                         	
	.Xtrig_rstn				(Xtrig_rstn			),//reg152
	.TFPP_time 				(TFPP_time			),
	.TRPP_time				(TRPP_time			),
	.LoopExp				(LoopExp			),
	.ExpTime				(ExpTime			),
	.isFrameEndlong			(framed_long		),   //input isFrameEndlong	
	        	
	.hsync_stled			(0					),//input   	wire [15:0] 	 
	.hsync_edled 			(0					),//input   	wire [15:0] 	 
	.m6inline_leden			(0					),//input   	wire 			
	.m6inline_st  			(0					),//input   	wire [15:0] 	
	.m6inline_ed			(0					),//input   	wire [15:0] 

//cmos ctrl        
	.CmosCtrlMode           (CmosCtrlMode		),
	.CMOS_CTL1_IN			(CMOS_CTL1_IN		),//SC2210:PWDNB input power down actice low 
  	.CMOS_CTL2_IN			(CMOS_CTL2_IN		),//SC2210:SD1 
  	.CMOS_CTL3_IN			(CMOS_CTL3_IN		),//SC2210:SD0
  	.CMOS_CTL4_IN			(CMOS_CTL4_IN		),
  	.CMOS_CTL5_IN			(CMOS_CTL5_IN		),
  	.CMOS_XHS_IN			(CMOS_XHS_IN		),//input XHS
  	.CMOS_XVS_IN			(CMOS_XVS_IN		),//input XVS 
  	.CMOS_AMPV_IN			(CMOS_AMPV_IN		), 	
  	.CMOS_TECEN_IN			(CMOS_TECEN_IN		),	
  	.CMOS_XCLR_IN			(CMOS_XCLR_IN		),
  	.CMOS_XMASTER_IN		(CMOS_XMASTER_IN	),
 	.CMOS_XTRIG_IN			(CMOS_XTRIG_IN		),
  		
  	.CMOS_TECEN_OUT			(CMOS_TECEN_OUT		),			
  	.CMOS_TECEN_OE			(CMOS_TECEN_OE		),
  	.CMOS_XTRIG_OUT			(CMOS_XTRIG_OUT		),//SC2210:EFSYNC trig   input 
	.CMOS_XTRIG_OE			(CMOS_XTRIG_OE		),
	.CMOS_XMASTER_OUT		(CMOS_XMASTER_OUT	),//output CMOS XMASTER 		
  	.CMOS_XMASTER_OE		(CMOS_XMASTER_OE	),
  	.CMOS_AMPV_OUT			(CMOS_AMPV_OUT		),//output AMPV
  	.CMOS_AMPV_OE			(CMOS_AMPV_OE		),
  	.CMOS_XCLR_OUT			(CMOS_XCLR_OUT		),//output XCLR 	SC2210 :Active low 	input 
  	.CMOS_XCLR_OE			(CMOS_XCLR_OE		),
  	.CMOS_XHS_OUT			(CMOS_XHS_OUT		),//SC2210:LREF DVP line sync output TO FPGA
  	.CMOS_XHS_OE			(CMOS_XHS_OE		),
  	.CMOS_XVS_OUT			(CMOS_XVS_OUT		),//SC2210:FSYNC DVP frame sync output TO FPGA
  	.CMOS_XVS_OE			(CMOS_XVS_OE		),
  	.CMOS_CTL1_OUT			(CMOS_CTL1_OUT		),
  	.CMOS_CTL1_OE			(CMOS_CTL1_OE		),
  	.CMOS_CTL2_OUT			(CMOS_CTL2_OUT		),
  	.CMOS_CTL2_OE			(CMOS_CTL2_OE		),
  	.CMOS_CTL3_OUT			(CMOS_CTL3_OUT		),
  	.CMOS_CTL3_OE			(CMOS_CTL3_OE		),
  	.CMOS_CTL4_OUT			(CMOS_CTL4_OUT		),
  	.CMOS_CTL4_OE			(CMOS_CTL4_OE		),
  	.CMOS_CTL5_OUT			(CMOS_CTL5_OUT		),
  	.CMOS_CTL5_OE			(CMOS_CTL5_OE		),
		
//output 	                                   	
	.expCounter				(					),	
	.trigout_gpio			(trigout_gpio		),
	.trigout_optic			(optic_trigout		),		
	.trigin_or_idle  		(trigin_or_idle		),	
	.mode6_led 				()	//output  	wire   			mode6_led 		    	                                    	
	
	                    	
	
);




//****************PIX DATA ****************************************************************

 mipicsi_rxctrl 	mipicsi_rxctrl_inst(
		
		.mipi_pclk				(syspll_CLKOUT100			),
		.mipi_rstn				(mipi_rstn					),
		                                                                                                       
  // MIPI Video input                                   
  		.mipi_rx_inst1_CNT		(mipi_rx_inst1_CNT		    ),
  		.mipi_rx_inst1_DATA		(mipi_rx_inst1_DATA		    ),
  		.mipi_rx_inst1_ERROR	(mipi_rx_inst1_ERROR		),
  		.mipirxip_hsync			(mipi_rx_inst1_HSYNC		),
  		.mipi_rx_inst1_TYPE		(mipi_rx_inst1_TYPE		    ),
  		.mipi_rx_inst1_ULPS		(mipi_rx_inst1_ULPS		    ),
  		.mipi_rx_inst1_ULPS_CLK	(mipi_rx_inst1_ULPS_CLK	    ),
  		.mipi_rx_inst1_VALID	(mipi_rx_inst1_VALID		),
  		.mipi_rx_inst1_VC		(mipi_rx_inst1_VC		    ),
  		.mipirxip_vsync			(mipi_rx_inst1_VSYNC		),
  		.DecodeMode				(DecodeMode					),
		.xpatch					(xpatch						),//2023 08 03 
                                                        
  // MIPI Control  output                                     
  		//.mipi_rx_inst1_CLEAR	(							),//mipi_rx_inst1_CLEAR clear error register
  		.mipi_rx_inst1_DPHY_RSTN(mipi_rx_inst1_DPHY_RSTN	),
  		.mipi_rx_inst1_LANES	(mipi_rx_inst1_LANES		),//2'B11
  		.mipi_rx_inst1_RSTN		(mipi_rx_inst1_RSTN		    ),
  		.mipi_rx_inst1_VC_ENA	(mipi_rx_inst1_VC_ENA	    ),//mipi_rx_inst1_VC_ENA
  		.DetectedXSize			(DetectedXSize				),
  		.DetectedYSize			(DetectedYSize				),
  		.DetectedBw				(DetectedBw					),
  	    .mipi_rxdata_o			(mipi_rxdata				),
  		.mipi_rxvld_o			(mipi_rxvld		    		),
  		.mipi_rxvsyncvld 		(mipi_rxvsyncvld    		),
  		.mipi_rxhsyncvld 		(mipi_rxhsyncvld    		),
  		.line_st				(mipi_line_st				),
  		.line_ed				(mipi_line_ed				),
  		.frame_st				(frame_st					),
  		.frame_ed  				(frame_ed  					), 
  		.framed_long  			(framed_long				)
);
//mipicsi_rxctrl    



 data_process	data_process_inst(

	.clk						(syspll_CLKOUT100			),
	.rst						(0							),//bypass Dgain_process,active high 

	.is16bit					(is16bit					),
	.mipi_rxdata				(mipi_rxdata				),
	.mipi_rxvld					(mipi_rxvld					),
	.mipi_line_ed				(mipi_line_ed				),
	.frame_st					(frame_st					),
	.frame_ed					(frame_ed					),
	.gain_19					(gain_19					),//REG19 
	.gain_18					(gain_18					),//REG18 
	.gain_20					(gain_20				    ),//REG20 
	.gain_21					(gain_21					),//reg21
	.gain_mode					(gain_mode					),
	.mipi_rx_inst1_TYPE			(mipi_rx_inst1_TYPE			),
	.mipi_rx_inst1_VC			(mipi_rx_inst1_VC			),//2024 12 25	VC	
	
	//22 06 02 add burst mode ,patch number 
	.ResetFrameCount			(trigin_or_idle				),//IDLE 
	.FrameNumEn					(FrameNumEn					),///reg56
	             
	.EnableBurstMode 			(EnableBurstMode			),/// reg57
	.PatchVNumber				(PatchVNumber				),///reg44-reg41> 0-31
	.BurstStart					(BurstStart					),///reg50  
	.BurstEnd  					(BurstEnd					),///reg52-reg51>0-15
	              
	.ddr_pfull					(ddr_code[5]				),///
	.isddr						(isddr						),
	//220531 gps data 
	.gps_enable					(FrameNumEn					),///GPS ENABLE 
	.DetectedYSize				(DetectedYSize				),///
	.DetectedXSize				(DetectedXSize				),///
	.gps1						(gps1						),
	.gps2						(gps2						),
	.gps3						(gps3						),
	.gps4						(gps4						),
	.gps5						(gps5						),
	.gps6						(gps6						),
	.gps7						(gps7						),
	.gps8						(gps8						),
	.gps9						(gps9						),
	
	.Data64_after				(Data64_after1				),
	.Data64Wr_after				(Data64Wr_after1			)
		
);
//data_process  

`ifdef DDRTEST_DATA
wire  [63:0] 	 data	;
wire  	   		 vld	;
wire			verify_err;

 testddr_data	testddr_data_inst(

	.clk		(syspll_CLKOUT100),
	.rst		(~ddr_rstn|DDR_CLG_R),
	.en			(ddr_code[0]	 ),
	.rd_ddrdata	(rddr3_data		 ),
	.rd_ddrvld	(rddr3_vld		 ),
                                                        
	.data	    (data			 ),
	.vld		(vld			 ),
	.verify_err	(verify_err		 )

);

`endif

 Ddr3Mig_Top  Ddr3Mig_Top_inst(

		//input
		.clk					(syspll_CLKOUT100		),//100M  syspll_CLKOUT100
		.clk_low				(						),//2Mhz -8Mhz   ddrpll_CLKOUT8Mhz
		.rstn					(ddr_rstn				),//active high  
		.DDR_CLG_R			    (DDR_CLG_R				),
		.pll_locked 			(syspll_LOCKED			),		
		.pll_ddr_locked 		(ddrpll_LOCKED			),
		.skip_check				(skip_check				),
//ddr3 signals 					
		.wr_clk			        (syspll_CLKOUT100		), 
		
`ifdef DDRTEST_DATA		
		.wrddr3_data		    (data					), //[63:0]	 Data64_after1   
		.wrddr3_en		        (vld					), //Data64Wr_after1
`else 
		.wrddr3_data		    (Data64_after1			), //[63:0]	 Data64_after1   
		.wrddr3_en		        (Data64Wr_after1		), //Data64Wr_after1	
`endif		
		.rd_clk			        (syspll_CLKOUT100		), 
		.rddr3_en		        (1'b1					), //enable read 
		.fx3_full		        (flag_full				), 
		.threshold_num	        (threshold_num			), // [DDR3ADDRE_MAXBIT:0]
		.rddr3_data		        (rddr3_data				), //output [63:0]	
		.rddr3_vld		        (rddr3_vld				), //output
        
        .rema_num         		(rema_num				),//-[DDR3ADDRE_MAXBIT:0]
        .ddr_code               (ddr_code				), // {ddr_pfull,ddr_alempty,ddr_alfull,addr_err,check_fail,check_done_t2};                                                                                                
//DDR hardcore signals                             
		//.DDR_CTRL_CFG_SDA_OEN	(   ), ////input  DDR_CTRL_CFG_SDA_OEN
 		//.DDR_CTRL_CFG_SCL_IN	(	), //output  DDR_CTRL_CFG_SCL_IN
  		//.DDR_CTRL_CFG_SDA_IN	(	), //output DDR_CTRL_CFG_SDA_IN
  		
  		.DDR_CTRL_CFG_SEQ_RST	(DDR_CTRL_CFG_SEQ_RST	),
  		.DDR_CTRL_CFG_SEQ_START	(DDR_CTRL_CFG_SEQ_START	),
    	.DDR_CTRL_CFG_RST_N		(DDR_CTRL_CFG_RST_N	    ),
    	
	    //input                                          
  		.DDR_CTRL_AREADY_0		(DDR_CTRL_AREADY_0		),  //**Address ready.
  		.DDR_CTRL_BID_0			(DDR_CTRL_BID_0			),  //Response ID tag. This signal is the ID tag of the write response.
  		.DDR_CTRL_BVALID_0		(DDR_CTRL_BVALID_0		),  //Write response valid. This signal indicates that the channel is signaling a valid write response.                  
  		.DDR_CTRL_RDATA_0		(DDR_CTRL_RDATA_0		),  //**Read data.              
  		.DDR_CTRL_RID_0			(DDR_CTRL_RID_0			),  //Read ID tag. This signal is the identification tag for the read data group of signals generated by the slave.        
  		.DDR_CTRL_RLAST_0		(DDR_CTRL_RLAST_0		),  //**Read last. This signal indicates the last transfer in a read burst.                      
  		.DDR_CTRL_RRESP_0		(DDR_CTRL_RRESP_0		),  //Read response. This signal indicates the status of the read transfer
  		.DDR_CTRL_RVALID_0		(DDR_CTRL_RVALID_0		),  //**Read valid
  		.DDR_CTRL_WREADY_0		(DDR_CTRL_WREADY_0		),  //**Write ready. This signal indicates that the slave can accept the write data.
  		//output                   		                                                                                       
  		.DDR_CTRL_AADDR_0		(DDR_CTRL_AADDR_0	    ),  //** Address. ATYPE defines whether it is a read or write address. It gives the address of the first transfer in a burst transaction. 
  		.DDR_CTRL_ABURST_0		(DDR_CTRL_ABURST_0	    ),  // Burst type. The burst type and the size determine how the address  for each transfer within the burst is calculated.               
  		.DDR_CTRL_AID_0			(DDR_CTRL_AID_0		    ),  // Address ID. This signal identifies the group of address signals. Depends on ATYPE, the ID can be for a read or write address group             
  		.DDR_CTRL_ALEN_0		(DDR_CTRL_ALEN_0		),  // Burst length. This signal indicates the number of transfers in a burst
  		.DDR_CTRL_ALOCK_0		(DDR_CTRL_ALOCK_0	    ),  // Lock type. This signal provides additional information about the  atomic characteristics of the transfer  
  		.DDR_CTRL_ASIZE_0		(DDR_CTRL_ASIZE_0	    ),  // Burst size. This signal indicates the size of each transfer in the burst.                       
  		.DDR_CTRL_ATYPE_0		(DDR_CTRL_ATYPE_0	    ),  //** This signal distinguishes whether is it is a read or write operation. 0= read and 1 = write.
  		.DDR_CTRL_AVALID_0		(DDR_CTRL_AVALID_0	    ),  //** Address valid. This signal indicates that the channel is signaling valid address and control information.                                                
  		.DDR_CTRL_BREADY_0		(DDR_CTRL_BREADY_0	    ),  // Response ready. This signal indicates that the master can accept a write response                            
  		.DDR_CTRL_RREADY_0		(DDR_CTRL_RREADY_0	    ),  //** Read ready. This signal indicates that the master can accept the read data and response information.                                              
  		.DDR_CTRL_WDATA_0		(DDR_CTRL_WDATA_0	    ),  //** Write data.
  		.DDR_CTRL_WID_0			(DDR_CTRL_WID_0		    ),  // Write ID tag. This signal is the ID tag of the write data transfer
  		.DDR_CTRL_WLAST_0		(DDR_CTRL_WLAST_0	    ),  //** Write last. This signal indicates the last transfer in a write burst
  		.DDR_CTRL_WSTRB_0		(DDR_CTRL_WSTRB_0	    ),  // Write strobes. This signal indicates which byte lanes hold valid data. There is one write strobe bit for each eight bits of the write data bus  
  		.DDR_CTRL_WVALID_0   	(DDR_CTRL_WVALID_0      )   //** Write valid. This signal indicates that valid write data and strobes are  available   
	                                 
);

//Ddr3Mig_Top_inst


 ddrorbypass	ddrorbypass_inst (
			
	.sclk				(syspll_CLKOUT100	),
`ifdef FX3TXNUMTEST
	.rst				(DDR_CLG_R			),
`endif 
	.isddr				(isddr			    ),	
	//.framest			(frame_st			),
	//.frameed			(frame_ed			),	
`ifdef FX3TESTDATA_Gen
	.diretly_data		(Fx3TestData	    ),//Fx3TestData  //Data64_after1
	.diretly_vaild		(Fx3TestDVld    	),//Fx3TestDVld	 //Data64Wr_after1	
`else 
	.diretly_data		(Data64_after1	    ),//Fx3TestData  //Data64_after1
	.diretly_vaild		(Data64Wr_after1    ),//Fx3TestDVld	 //Data64Wr_after1	
`endif					
	.ddr_data			(rddr3_data	    	),
	.ddr_vld			(rddr3_vld			),
                 	                   	                                                        
	.fx3_data			(fx3_wr_data		),//fx3_wr_data
	.fx3_vaild 			(fx3_wr_vaild 		)
	//.fx3txnum64			(			)
								

);
// ddrorbypass

fx3_tx fx3_tx_inst (

         .out_clk			  	(syspll_CLKOUT100   ),//200mhz clk_o200 clk_o200 clk_out3
         .out_data			  	(fx3_wr_data       	),//input   cmos  fx3_wr_data
         .out_data_valid	  	(fx3_wr_vaild  	    ),//input   cmos  fx3_wr_vaild
         .fx3_clk			  	(syspll_CLKOUT100   ),//100mhz
         
         .fx3_rdy			  	(fx3_ctl1		    ),//input
         .fx3_slwr			  	(fx3_ctl12		    ),//output
         .fx3_data			  	(fx3_data           ),//fx3_data output
         .fx3_pkend			  	(fx3_ctl11		    ),//output
         .flag_full			  	(flag_full		    ) //output
   );
////fx3_tx

//==============================================================================================================================//

 gps_decoder 	gps_decoder_inst(

	.gps_clk		(syspll_CLKOUT25	),
	.pix_clk		(syspll_CLKOUT100	),
	.gps_rst		(0					),//active high 
	.gps_in			(GPSBOX_DataReceived),
		
	.gps1			(gps1				),
	.gps2			(gps2				),
	.gps3			(gps3				),
	.gps4			(gps4				),
	.gps5			(gps5				),
	.gps6			(gps6				),
	.gps7			(gps7				),
	.gps8			(gps8				),
	.gps9			(gps9				)

);
//gps_decoder  22 05 31


 MuxGpio_SignalSwitch 	MuxGpio_SignalSwitch_inst(

		.enable					(TrigSignalEn),
		.TrigOut_IN				(trigout_gpio),// gpio trig out 
		.ShutterMessure_IN		(),//VSYNC
		.SYNC_IN				(),//HSYNC

		.guide1					(guide1_gps_ctl),
		.guide2					(guide2),
		.guide3					(guide3),
		.guide4					(guide4),
		
		.slaveXVS				(),//VSYNC						
		.GPSBOX_Control			(guide1_gps_ctl),
		.GPSBOX_CLK				(syspll_CLKOUT25),
															                  								
		//select the different working mode 
		.mode					(test_mode),
		
		//from the gpio 5pin input 
		.mode6_led				(),
		.gpio_in1				(MUX_GPIO1_IN	),//MUX_GPIO1_IN
		.gpio_in2				(MUX_GPIO2_IN	),
		.gpio_in3				(MUX_GPIO3_IN	),
		.gpio_in4				(MUX_GPIO4_IN	),
		
		//output														
		.oe1					(MUX_GPIO1_OE),//MUX_GPIO1_OE  MUX_GPIO1_OE
		.oe2					(MUX_GPIO2_OE),
		.oe3					(MUX_GPIO3_OE),
		.oe4					(MUX_GPIO4_OE),												
		.out1					(MUX_GPIO1_OUT),//MUX_GPIO1_OUT    MUX_GPIO1_OUT
		.out2					(MUX_GPIO2_OUT),
		.out3					(MUX_GPIO3_OUT),
		.out4					(MUX_GPIO4_OUT),
								
		//the following is the signal from the switch network from the gpio_in1,2,3,4
		.GPSBOX_DataReceived	(GPSBOX_DataReceived),
		.HSYNC_SlaveIn			(),//not use  2020 0420 
		.VSYNC_SlaveIn			(),
		.trigin_gpio			(trigin_gpio)
);
//MuxGpio_SignalSwitch		22 05 30				
						

 flash_ctrl			flash_ctrl_inst(

		.clk  					(syspll_CLKOUT25),//clk25M  syspll_CLKOUT25
		.rst					(asmi_reset		),//active high reg115  
		.flash_cs				(asmi_flashcs	),//FLASH CS REG132
		.asmi_address			(asmi_address	),//reg108-reg106
		.asmi_num				(asmi_num		),//reg135
		.asmi_writestart		(asmi_writestart),//write start reg140
		.asmi_readstart			(asmi_readstart	),//read start  reg112
		.asmi_erasestart		(asmi_erasestart),//erase start  reg116
		.asmi_didata			(asmi_didata	),//AdataToRegIF
		.asmi_divld				(asmi_divld		),//AwriteEn
		.asmi_wpen				(asmi_wpen		),//asmi_wpen reg117
					        	                              
		.asmi_dataout			(asmi_dataout	),//myreg205
		.asmi_dovalid			(asmi_dovalid	),
		.busy					(busy			),
			                                                                                          
//SPI ports  		                               
		.miso 					(miso 		    ),	
		//.miso_1 		    	( 				),//miso_1
		//.miso_2 		    	( 				),//miso_2
		//.miso_3 		    	( 				),//miso_3
		                    	           
		.sclk 					(sclk 		    ),
		.CS0 					(CS0 		    ),
		.CS1					(CS1			),
		.mosi 					(mosi_OUT 		 ),
		.HOLD_N					(HOLD_N			),
		.WP_N					(WP_N			),
		//.mosi_1 		    	( 				),//mosi_1
		//.mosi_2 		    	( 				),//mosi_2
		//.mosi_3 		    	( 				),//mosi_3
		.mosi_oe 				(mosi_OE 	    		),//mosi_oe
		//.mosi_oe1				(				),//mosi_oe1
		//.mosi_oe2				(				),//mosi_oe2
		//.mosi_oe3				(				),//mosi_oe3
						                           
//reconfigure 			                           
		.remote_reconfig		(remote_reconfig),//reg136
		.address_sel			(remote_sel		),//reg133			
		                    	
		.cfg_CBSEL				(cfg_CBSEL		),//
  		.cfg_CONFIG				(cfg_CONFIG		),
  		.cfg_ENA				(cfg_ENA		)
);
// flash_ctrl

 watchdogs	watchdogs_INST(

		.clk					(syspll_CLKOUT25			),//25M 40ns
		.watchdogenable			(watchdogenable				),//connect to reg89,reg89 initial value is 1
		.driver_feed_dogs		(driver_feed_dogs			),//connect to reg1x03 ,reg1x03 initial value is 0	
		                  
		.FX3_RST_N				(fx3_resetn					),//FX3_RST_N
		.rstn_num				(rstn_num					) //connect to myreg 210;
		
);
//watchdogs 22 05 29

 fpga_info	fpga_info_inst(

		.year					(year		    ),
		.month					(month		    ),
		.day					(day			),
		.subversion1			(subversion1	),
		.subversion2			(subversion2	),
		.boardty     			(boardty        )
	
);
//fpga_info
//wire  [7:0] reg31;
//wire  [7:0] reg32;

 LED_state LED_state_inst(

	.clk			(syspll_CLKOUT25),//25M clk  40ns 
	.rst			(0				),
	.LED_control	(LED_control	),
	.subversion1	(subversion1	),//subversion1
	.fpga_state1	(fpga_state1	),//fpga_state1    cfg_ERROR ;MIPI Ecc no error;check_fail  
	.fpga_satte2	(0				),
	.fpga_state3 	(0				),
	                                  
	.LED_R			(LED_R			),
	.LED_G			(LED_G			),
	.LED_B			(LED_B			)
			
);  
//LED_state

i2cSlave 	i2cSlave0_inst(

		.clk					(syspll_CLKOUT25),
		.rst					(1'b0			),
		.sdaIn					(I2CSDA_0_IN	),
		.scl					(I2CSCL_0		),
		.sdaOut  				(I2CSDA_0_OUT	),
		.sda_oe      			(I2CSDA_0_OE	),        
		                	                              
		.myreg00				(DetectedXSize[15:8]),
		.myreg01				(DetectedXSize[7:0]),
		.myreg02				(DetectedYSize[15:8]),
		.myreg03				(DetectedYSize[7:0]),
		.myreg04				(rema_num[12:5]),///test  fx3txnum64[7:0] rema_num is byte ,set it to 256bit,is X <<3 >>8=X>>5     		       		
		.myreg05				(rema_num[20:13]),//fx3txnum64[15:8]
		.myreg06				(rema_num[`DDR3ADDRE_MAXBIT:21]),//fx3txnum64[23:16]
		
	                    	 	           		       	
		.myreg28				(), //test  fx3txnum64[31:24]
		
		.myreg29				(),//not use 
		.myreg30				(), //not  use  
		.myreg31				(), //not use
		.myreg32				(), //not use  
		.myreg33				(), //not use  
		
		.myreg41				(DetectedBw[7:0]),//
		.myreg42				(DetectedBw[15:8]), // 
		
		.myreg52				(fpga_state1	),
		.myreg53				(fpga_state2	),
		.myreg54				(fpga_state3	),//fpga_state3
		.myreg55				(	),//fpga_state4
		.myreg56				(	),//fpga_state_indicate
		            
		.myreg200				(year), //FPGA verson year
		.myreg201				(month), //month
		.myreg202				(day), //day
		.myreg203				(subversion1), //subversion1
		.myreg204				(boardty), //bord type
		.myreg205				(asmi_dataout),
		.myreg206				(),
		.myreg207				(subversion2),//subversion2
        .myreg210				(rstn_num),                	           
                                                                                                         
		.reg00					(i2c_xclr),
		.reg01					({skip_check, regddr_rstn}),// 2ms
		.reg02					(),
		.reg03					(is16bit),
		.reg04					(guide1_gps_ctl),// 
		.reg05					(guide2),
		.reg06					(guide3),
		.reg07					(guide4),
		.reg08					(ampv_enable),// 
		.reg09					(AMPV_END[15:8]), //8
		.reg10					(AMPV_END[7:0]),
		.reg11					(),
		.reg12					(AMPV_END[31:24]),
		.reg13					(AMPV_END[23:16]),
		.reg14					(AMPV_START[15:8]),
		.reg15					(AMPV_START[7:0]),
		.reg16					(AMPV_START[31:24]),
		.reg17					(AMPV_START[23:16]),
		.reg18					(gain_18),//red
		.reg19					(gain_19),//green
		.reg20					(gain_20),//green 
		.reg21					(gain_21),//blue 
		.reg22					(VMAX[31:24]),
		.reg23					(VMAX[23:16]),
		.reg24					(VMAX[15:8]),
		.reg25					(VMAX[7:0]),
		.reg26					(HMAX[31:24]),//8
		.reg27					(HMAX[23:16]),
		.reg28  				(HMAX[15:8]),
		.reg29					(HMAX[7:0]),
		.reg30			        (isddr),
		.reg31					(xpatch),//
		.reg32					(),//8
		.reg34					(),//1
		.reg35					(IDLE),
		.reg36					(LoopExp),//1
		.reg37					(),//8
		.reg38					(),
		.reg39					(test_mode),
		.reg40					(LED_control),
		.reg41					(PatchVNumber[31:24]),
		.reg42					(PatchVNumber[23:16]),
		.reg43					(PatchVNumber[15:8]),
		.reg44					(PatchVNumber[7:0]),
		.reg45					(VMAX_2_LSB[15:8]),		
		.reg46					(VMAX_2_LSB[7:0]),	//8
	                        	
		.reg49					(AMPV_MANUAL),//1       //manual AMPV                           		                            		
		.reg50					(BurstStart),//8
		.reg51					(BurstEnd[15:8]),
		.reg52					(BurstEnd[7:0]),
		.reg53					(gain_mode),
		.reg54					(ac_mipi_rstn),//ac_mipi_rstn 1ms 
		.reg55					(),//8
		.reg56					(FrameNumEn),//1       //FRAME COUNTER ENABLE
		.reg57					(EnableBurstMode),//1
		.reg58					(TrigMode),//8
		.reg59					(),
		.reg60					(),
		.reg61					(),
		.reg62					(DecodeMode),//DecodeMode
		.reg63					(DDR_CLG_R),
		.reg64					(),
		.reg65					(threshold_num[7:0]),
		.reg66					(threshold_num[15:8]),
		.reg67					(threshold_num[23:16]),
		.reg68					(threshold_num[`DDR3ADDRE_MAXBIT:24]),
		.reg69					(),
		.reg70					(),
		.reg71					(),
		.reg72					(),
		.reg73					(),
		.reg74					(),
		.reg75					(),//8
		.reg76					(),//2
		.reg77					(),//2
		.reg79					(CmosCtrlMode),
    	                    	                                                
		.reg80					(),//8  //#1 PLL Dynamic Phase Adjustment C0-C4 Select
		.reg81					(),//1		   //direction
		.reg82					(),        //run
		.reg83					(),//1        //pll reset
		.reg84					(),//8  //#2 PLL DPA C0-C4
		.reg85					(),//1        //direction
		.reg86					(),        //run
		.reg87					(),//1        //pll reset
		.reg88					(),//8  //LVDS channel Select
		.reg89					(watchdogenable),//1
	                    		
		.reg91					(),//8  //LVDS input phase delay time
		.reg92					(),//1        //LVDS input phase delay time WR  0->1 action
		.reg93					(),//8  //LVDS bit shift. bit select position
		.reg94					(),//1        //LVDS bit select WR  0->1 action
		.reg95					(),//1        //LVDS input phase detector exe  0:reset/clear  1:run  
		.reg96					(),//8
		.reg97					(),//1
		.reg98					(),//1
		       
		.reg1x03             	(driver_feed_dogs),
		.reg1X05				(),//8  //asmi_addr[31- T35 not use 
		.reg1X06				(asmi_address[23:16]),  //asmi_addr[23-
		.reg1X07				(asmi_address[15:8]),  //asmi-addr[15-
		.reg1X08				(asmi_address[7:0]),//8  //asmi-addr[7-
		.reg1X09				(),//1         //Bulk_earse  not use 
		.reg1X10				(),//8   //datain [7-] not use 
		.reg1X11				(asmi_rden),//1
		.reg1X12				(asmi_readstart),
		.reg1X13				(),//not use
		.reg1X14				(), //not use 
		.reg1X15				(asmi_reset),
		.reg1X16				(asmi_erasestart),
		.reg1X17				(asmi_wpen),//
		.reg1X18				(),//not use 
		.reg1X19				(),//not use 
		.reg1X20				(),//1
	                        	
		.reg1X29				(),//not use 8
		.reg1X30				(),//not use 
		.reg1X31				(),//not use 
		.reg1X32				(asmi_flashcs[1:0]),//
		.reg1X33				(remote_sel),//8
		.reg1X34				(),//not use 1
		.reg1X35				(asmi_num),//8
		.reg1X36				(remote_reconfig),//1
		.reg1X37				(),//not use
		.reg1X38				(),//not use
		.reg1X39				(),//not use
		.reg1X40				(asmi_writestart),//1
		.reg1X41				(),//8   not use    
		.reg1X42				(TrigSignalEn),  
		.reg1X43                (AMPV_MODE),
		.reg1X46				(FilterTime[23:16]),
		.reg1X47				(FilterTime[15:8]),
		.reg1X48  				(FilterTime[7:0]),		
		.reg1X52				(Xtrig_rstn),
		.reg1X53       			(TrigModeA),     
		.reg1X54				(ExpTime[39:32]),
		.reg1X55				(ExpTime[31:24]),
		.reg1X56				(ExpTime[23:16]),    
		.reg1X57				(ExpTime[15:8]),    
		.reg1X58				(ExpTime[7:0]),   
		.reg1X59				(TFPP_time[7:0]),//TFPP_time
		.reg1x60 				(TFPP_time[15:8]),
		.reg1x61 				(TFPP_time[23:16]), 
		.reg1x62 				(TFPP_time[31:24]),
		.reg1x63 				(TRPP_time[7:0]),//TRPP_time
		.reg1x64 				(TRPP_time[15:8]),
		.reg1x65 				(TRPP_time[23:16]),
		.reg1x66				(TRPP_time[31:24]),

		.reg2X55				(),//8 not use 
		.AwriteEn				(asmi_divld),//1
		.AregAddr				(),//8
		.AdataToRegIF			(asmi_didata) //8

);
//i2cSlave


endmodule