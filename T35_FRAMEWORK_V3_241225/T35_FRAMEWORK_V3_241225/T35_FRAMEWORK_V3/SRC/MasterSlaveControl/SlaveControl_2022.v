`timescale 1ns / 1ps
//-------------------------------------------------------------------------------
// Company:  QHYCCD
// Engineer: YangSK
// 
// Create Date: 2022/6/9
// Design Name: T35_TOP
// Module Name: T35_TOP
// Project Name: T35_FRAMEWORK
// Target Devices: t35f324
// Tool Versions: EFINITY21.2
// Description: set master mode or slave mode ,Integrate previous modules£¬include xTrigExposure.v £»masterModeAMPV.v;
// Dependencies:  Add AMPV signal selection,USE AMPV_MODE
// 
// Revision:rev1
// 
// Additional Comments:
// 
//--------------------------------------------------------------------------------


module SlaveControl_2022(

					input wire			clk_in				, 
					input wire			IDLE_ctrl			, 
					input wire			ampv_enable			,	
					input wire			AMPV_MANUAL			,//reg49
					input wire [7:0]	AMPV_MODE			,//reg143													
					input wire [31:0] 	VMAX				,
					input wire [31:0] 	HMAX				,					
					input wire [31:0] 	AMPV_START			,
					input wire [31:0] 	AMPV_END			,																																										                          																
					input wire [15:0] 	VMAX_2_LSB			,					
					input wire [7:0] 	burst_start 		,
					input wire [15:0]   burst_end 			,
					input wire			EnableBurstMode 	,	
									
					input wire [15:0]   hsync_stled			,//  GPIO mode6 led
					input wire [15:0]   hsync_edled 		,//  GPIO mode6 led
					input wire 		    m6inline_leden		,
					input wire [15:0]   m6inline_st  		,
					input wire [15:0]   m6inline_ed			,
				    output reg   		mode6_led=0 		,
				    
					output reg 			ampv_out			,				
					output reg   		trigout_slave 		,
					output reg  		xvs_out=0 			,
					output reg  		xhs_out=0 			,		
				
					
					//********xTrigExposure*************************
					
					input	wire		Xtrig_rstn			,//REG152					
					input 				LoopExp				,//REG36 						
					input [39:0] 		ExpTime				,//unit 40ns			
					input 				isFrameEndlong		,
					input [31:0]		TFPP_time 			,// trig fall prohibited period
					input [31:0]		TRPP_time			,// trig rise prohibited period
																							
					output reg 			XTRIG				,
					output wire			Xtrig_out			,		
					output reg [39:0] 	expCounter			,
					
					//************masterModeAMPV********************
					
					input wire			xhs_in				,
					input wire			xvs_in				
					
);

reg 		slave_ampv	=1;
reg [31:0] 	pix_count = 0;
reg [31:0] 	frame_count =0;
wire[31:0] 	h_count		;

		
reg 		rst_D0				;   
reg 		rst_D1				;
reg			rst=0				;	
reg [31:0]	h_count_a  =0		;
reg 		trig_out_a =0		;	


always @(posedge clk_in) 
begin
   rst <= IDLE_ctrl; 
   rst_D0<=rst;
   rst_D1<=rst_D0;
     
end 


always@(posedge clk_in) 
begin 
if(rst_D1==0)    pix_count<=0;
else 
  begin
  
    if (pix_count<HMAX)   pix_count<=pix_count+1;  //pixel_count range : 0 to HMAX 
	 else                 pix_count<=0;
  end
end
//pix_count

always @(posedge clk_in) 
begin
if(rst_D1==0)  xhs_out<=1;
else
   begin 
     if(pix_count<50 && pix_count>2)   xhs_out <=0;
	  else                             xhs_out <=1;
	end 
end
//xhs_out 
	 
	 	
always@(posedge clk_in) 
begin
if(rst_D1==0) xvs_out<=1;
else 
  begin 
    if(pix_count<60 && h_count==0)   xvs_out<=0;
    else                             xvs_out<=1;
  end 
end
//generate xvs_out_INTERNAL

always@(posedge clk_in) 
begin 
if(rst_D1==0)    h_count_a <=0;
else 
  begin
    if (pix_count==HMAX) 
    begin 
        
		  if(VMAX_2_LSB==0)
		  begin
	        if(h_count_a<VMAX)	         h_count_a<=h_count_a+1;     //h_count range : 0 to VMAX 
		     else                        h_count_a<=0;	

		  end 
		  
		  else 
		  begin 		  
		    if(frame_count[0]==1) 
		    begin
	          if(h_count_a<VMAX_2_LSB)	      h_count_a<=h_count_a+1;  //h_count range : 0 to VMAX 
				 else                      h_count_a<=0;
		    end
		    else
		    begin
	          if(h_count_a<VMAX)	      h_count_a<=h_count_a+1;  //h_count range : 0 to VMAX_2_LSB
				 else                     h_count_a<=0;
		    end  
	     end
	 
	 end
  end
end
/////h_count_a

assign h_count = h_count_a;

/////////////////////////////////////2020 08 04  ysk  



always@(posedge clk_in) 
begin
if(rst_D1==0) frame_count=0;
else 
  begin
    if (pix_count==1 && h_count==0) frame_count<=frame_count+1;
  end
end
//frame_count

  always@(posedge clk_in )begin
    if (EnableBurstMode)begin 
			if ((frame_count>burst_start)&&(frame_count<(burst_end)))begin 
					trig_out_a <= ~xvs_out;
			end else begin 
				   trig_out_a <= 0;
			end 
	  end else begin 
			trig_out_a <= ~ xvs_out ;
	  end 
	end 
//trig_out_a ,ACTIVE HIG


 always@(posedge clk_in )begin

 		trigout_slave <= trig_out_a;
 end 
//trigout_select

//***************TRIG OUT LONG END **********************************************

	  
always@(posedge clk_in) 
begin
if (rst_D1==0) slave_ampv <=1;
else
   begin 
	  //if(ampv_enable==0)   slave_ampv<=1;
	  //else 
	    begin
		    if     (h_count>=AMPV_START && h_count<=AMPV_END)     slave_ampv<=0;
			else                                                  slave_ampv<=1;
		 end 
	end
end

//****************************************mode6_led************************************************************
//reg 		mode6_lineled =0  	;	
//always@(posedge clk_in) begin 
//	if(EnableBurstMode==1)begin 
//			if(h_count>hsync_stled &&h_count<hsync_edled&&frame_count>burst_start&&frame_count<burst_end)begin 
//						mode6_lineled <= 1;
//			end else begin 
//						mode6_lineled <= 0;
//			end
//   end else if(h_count>hsync_stled &&h_count<hsync_edled)begin 
//				mode6_lineled <= 1;
//	end else begin 
//				mode6_lineled <= 0;
//	end
//end 
////mode6_lineled
//
//always@(posedge clk_in) begin 
//	if(m6inline_leden)begin
//		if((pix_count > m6inline_st)&&(pix_count<m6inline_ed))begin 
//			mode6_led <= mode6_lineled;
//		end else begin 
//			mode6_led <= 0;
//		end 
//    end else begin 
//    	mode6_led <=  mode6_lineled ;
//    end 
//end 
////mode6_led

//********************************************************************************************
//************************************mux_ampv************************************************
//********************************************************************************************

always@(posedge clk_in) begin 
	if(ampv_enable==0)begin//ampv_enable=reg08
		ampv_out<=1;
	end else begin
		case(AMPV_MODE[1:0])//AMPV_MODE=reg143
		0:ampv_out<=AMPV_MANUAL;//AMPV manaul control ,AMPV_MANUAL=reg49
		1:ampv_out<=master_ampv;//ampv start ,ampv end control in master mode (inpu xhs,xvs)
		2:ampv_out<=slave_ampv;//ampv start ,ampv end control in slave mode 
		3:ampv_out<=Xtrig_ampv;//starts automatically 10ms after  exposure £¬ closes automatically 10ms before the end of exposure,
		default:ampv_out<=slave_ampv;
		endcase
	end 

end 
//ampv_out
//********************************************************************************************
//************************************masterModeAMPV******************************************
//********************************************************************************************


reg master_ampv=1;							
reg [31:0] xhs_counter=0;	
						
reg xvs_in1;
reg xhs_in1;
reg xhs_pulse;
reg xvs_pulse;

always@(posedge clk_in) begin 

 xvs_in1<=xvs_in;
 xhs_in1<=xhs_in;
 
end

always@(posedge clk_in) begin 

if (xvs_in==0 && xvs_in1==1 )   xvs_pulse<=1;//falling edge
else                     		xvs_pulse<=0;

end 
//xvs_pulse

always@(posedge clk_in) begin 

if (xhs_in==0 && xhs_in1==1 ) xhs_pulse<=1;//falling edge
else                          xhs_pulse<=0;

end 
//xhs_pulse

	 
always@(posedge clk_in) begin
  if(xvs_pulse==1) 
  	xhs_counter<=0;
  else if(xhs_pulse==1)    
  	xhs_counter<=xhs_counter+1;
  else 
  	xhs_counter<=xhs_counter;
  
end	 
//xhs_counter	

//always@(posedge clk_in) begin
//
//if (ampv_enable==0) master_ampv <= 1;    
//else 
//  begin
//
//   if(xhs_counter<=AMPV_START) master_ampv<=1;
//	else  
//	  begin 
//	     if(xhs_counter>=AMPV_END)   master_ampv<=1;
//        else                        master_ampv<=0;
//	  end 
//	  
//  end
//end
//

always@(posedge clk_in) begin

	//if (ampv_enable==0) 
	//	master_ampv <= 1;    
	//else 
	if(xhs_counter<=AMPV_START&&xhs_counter>=AMPV_END) 
		master_ampv<=1;
	else                        
		master_ampv<=0;
end
//master_ampv



//********************************************************************************************
//************************************xTrigExposure*******************************************
//********************************************************************************************

localparam XIDLE 	= 4'b0001,
		   WaitExp	= 4'b0010,
		   onExp	= 4'b0100,
		   endExp	= 4'b1000;
localparam AmpvThreshold = 10_000_000/40;
reg [3:0]	Xtrig_State =0;
reg [31:0]  Min_IntervalCnt=1;
reg			LoopExpEn =0;
reg	[31:0]	TR_IntervalTime=0;
reg [31:0]	TF_IntervalTime=0;
reg Xtrig_ampv=1;

always @ (posedge clk_in)begin
	if(rst_D1==0|Xtrig_rstn==0)begin
		LoopExpEn<=LoopExp;
	end else begin
		LoopExpEn<=LoopExpEn;
	end 
end 
//loopexpen

always @ (posedge clk_in) begin
	if(Min_IntervalCnt[31]==1&&XTRIG==1)begin
	    Min_IntervalCnt<=Min_IntervalCnt;
	end else if(XTRIG)begin
		Min_IntervalCnt<=Min_IntervalCnt+1;
	end else begin 
		Min_IntervalCnt<=0;
	end 
end 
//Min_IntervalCnt

always @ (posedge clk_in)begin 
	if(ExpTime>TRPP_time)begin
		TR_IntervalTime<=TF_IntervalTime;//25000*40NS=1MS
	end else begin
		TR_IntervalTime<=TRPP_time-ExpTime;
	end 
end 
//TR_IntervalTime

always@ (posedge clk_in )begin
	TF_IntervalTime<=TFPP_time+16;//25000*40NS=1MS
end 
//TF_IntervalTime

always@(posedge clk_in)begin
	if(Xtrig_State==onExp)begin
		XTRIG<=0;
	end else begin
		XTRIG<=1;
	end 
end 
//xtrig

assign Xtrig_out=   ~XTRIG    ;

always@ (posedge clk_in)begin
	if(XTRIG==0)begin
		expCounter<=expCounter+1;
	end else begin 
		expCounter<=0;
	end 
end 
//expcounter

always @ (posedge  clk_in)begin
	if(rst_D1==0|Xtrig_rstn==0)begin
		Xtrig_State<=XIDLE;
	end else begin
	case(Xtrig_State) 
	XIDLE:begin	
		Xtrig_State<=WaitExp;	
	end
	WaitExp:begin
		if(Min_IntervalCnt>TR_IntervalTime&&Min_IntervalCnt>TF_IntervalTime)begin
			Xtrig_State<=onExp;
		end else begin
			Xtrig_State<=Xtrig_State;
		end 
	end 
	onExp:begin
		if(expCounter>(ExpTime-3))begin
			Xtrig_State<=endExp;
		end else begin 
			Xtrig_State<=Xtrig_State;
		end
	end 
	endExp:begin
		if(LoopExpEn)begin
			Xtrig_State<=WaitExp;
		end else begin
			Xtrig_State<=endExp;
		end 
	end 
	default:Xtrig_State<=XIDLE;
	endcase
	end 
end 




always@(posedge clk_in) 
begin 
  if ((expCounter>AmpvThreshold) && (expCounter<(ExpTime-AmpvThreshold))) 	Xtrig_ampv<=0;   //after 10ms enable ampv 
  else                                                						Xtrig_ampv<=1;   //disable ampv

end
//ampv

/*
// ysk ERIS QHY530 xTRIG MODE ,Does not work with current T35 transfer boards
localparam  IDLE = 8'd1;      //in idle statu, wait for the start exposure command
localparam  onExp = 8'd5;     //in onExp, wait the end of exposure 
localparam  waitReadout =8'd3; //in wait the isFrameEnd signal
//parameter  endExp = 8'd7;    //in end Exp. decide if return to idle or loop imaging


//10ms


//use the 25MHz input .  The time resolution is 40ns.
//when rst_D1 is 0 . camera is waiting the command
//when rst_D1 is 1 . camera begin to exposure and will end automaticly according the expTime setting

reg [3:0] CurrentState=1;
reg [3:0] NextState=1;
reg isFrameEnd_D1;


always@(posedge clk_in) 
begin 
    isFrameEnd_D1<=isFrameEndlong;      //cross-clock region resample.                          
end
//isFrameEnd_D1

always @ (posedge clk_in ) 
begin
  if(Xtrig_rstn==0)  expCounter<=0;
  else
   begin
    case (CurrentState)
	 IDLE:          expCounter<=0;
	 onExp:         expCounter<=expCounter+1;
	 waitReadout:   expCounter<=0;
		  
    default:       expCounter<=0;
    endcase
   end
end 
//expCounter

always@(posedge clk_in) 
begin 
  if (expCounter>0 && expCounter<ExpTime) XTRIG<=0;
  else                                    XTRIG<=1;

end
//XTRIG
 
assign Xtrig_out=   ~XTRIG    ;



always @(posedge clk_in )
begin

  if(Xtrig_rstn==0) CurrentState<=IDLE;
  else       CurrentState<=NextState;

end 
	
	
always @( * ) //CurrentState
begin
  case(CurrentState)
  IDLE:  
    begin
	    if(rst_D1==0) NextState=IDLE;
		 else                  NextState=onExp;
	 end 
  onExp:
    begin
	   if(expCounter<ExpTime) NextState=onExp;
		else                   NextState=waitReadout; 
	 end 
  waitReadout:
  
    begin
	   if(isFrameEnd_D1==1)  
	      begin 
			  if(LoopExp==0)     NextState=IDLE;//signal frame
			  else               NextState=onExp;//live frame 
         end 	
      else if(isFrameEnd_D1==0)  NextState=waitReadout;
    end 

	 
 // endExp:
	 
	 default:   NextState=IDLE; 
	
   endcase	
end 
//state machine for the xTrig Exposure Controller
*/



	
endmodule
