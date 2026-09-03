


module MuxGpio_SignalSwitch(

						input wire	enable	,
						input wire	TrigOut_IN,
						input wire  ShutterMessure_IN,//VSYNC
						input wire  SYNC_IN, // HSYNC

						input wire guide1,
						input wire guide2,
					    input wire guide3,
					    input wire guide4,
					
				        input wire slaveXVS,//vsync						
						input wire GPSBOX_Control,
						input wire GPSBOX_CLK,
																			                  								
						//select the different working mode 
						input wire [7:0] mode,
						
						//from the gpio 5pin input 
						input wire mode6_led,
						input wire gpio_in1,//MUX_GPIO1_IN
						input wire gpio_in2,
						input wire gpio_in3,
						input wire gpio_in4,
																					
						output reg oe1,//MUX_GPIO1_OE
						output reg oe2,
						output reg oe3,
						output reg oe4,												
						output reg out1,//MUX_GPIO1_OUT   
						output reg out2,
						output reg out3,
						output reg out4,
												
						//the following is the signal from the switch network from the gpio_in1,2,3,4
						output reg GPSBOX_DataReceived,
						output reg HSYNC_SlaveIn,//not use  2020 0420 
						output reg VSYNC_SlaveIn,
						output reg trigin_gpio=1
														
						);

wire slaveXHS;
wire GPSBOX_ShutterMessure;
wire TrigOut;

	assign TrigOut        			= enable ? TrigOut_IN        : 0 ;
	assign GPSBOX_ShutterMessure 	= enable ? ShutterMessure_IN : 0 ;
	assign slaveXHS           		= enable ? SYNC_IN           : 1 ;
	

always @( * )
begin
	
	case(mode[3:0])
	0:begin	out1=guide1;   				oe1=1;end 
	1:begin	out1=GPSBOX_Control; 		oe1=1;end 
	2:begin	out1=GPSBOX_ShutterMessure; oe1=1;end 
	3:begin	out1=GPSBOX_ShutterMessure; oe1=1;end 
	4:begin	out1=0;                     oe1=0;end 
	5:begin	out1=0;                     oe1=0;end 
	6:begin	out1=GPSBOX_ShutterMessure; oe1=1;end 
	default:begin out1=0;                     oe1=0;end 
	endcase
//    if         (mode[3:0]==0)    begin out1=guide1;                oe1=1; end 
//	else if   (mode[3:0]==1)    begin out1=GPSBOX_Control;        oe1=1; end
//	else if   (mode[3:0]==2)    begin out1=GPSBOX_ShutterMessure; oe1=1; end                        
// 	else if   (mode[3:0]==3)    begin out1=GPSBOX_ShutterMessure; oe1=1; end    
//	else if   (mode[3:0]==4)    begin out1=0;                     oe1=0; end    
//	else if   (mode[3:0]==5)    begin out1=0;                     oe1=0; end  
//	else if   (mode[3:0]==6)    begin out1=GPSBOX_ShutterMessure; oe1=1; end    
//	
//	else                        begin out1=0;                     oe1=0; end  
end 


always @( * )
begin
	case(mode[3:0])
	0:begin	 		out2=guide2;                      oe2=1;                              end 
	1:begin	 		out2=guide2;                      oe2=0; GPSBOX_DataReceived=gpio_in2;end 
	2:begin	 		out2=0;                           oe2=0; trigin_gpio= gpio_in2;          end 
	3:begin	 		out2=!GPSBOX_ShutterMessure;      oe2=1;                              end 
	4:begin	 		out2=0;                           oe2=0;                              end 
	5:begin	 		out2=0;                           oe2=0;                              end 
	6:begin	 		out2=0;                           oe2=0; trigin_gpio= gpio_in2;          end 
	default:begin 	out2=0;                           oe2=0; 							  end 
	endcase
//    if      (mode[3:0]==0)    begin out2=guide2;                      oe2=1;                              end
//	else if   (mode[3:0]==1)    begin out2=guide2;                      oe2=0; GPSBOX_DataReceived=gpio_in2;end
//	else if   (mode[3:0]==2)    begin out2=0;                           oe2=0; trigin_gpio= gpio_in2;          end
//	else if   (mode[3:0]==3)    begin out2=!GPSBOX_ShutterMessure;      oe2=1;                              end
//	else if   (mode[3:0]==4)    begin out2=0;                           oe2=0;                              end
//  else if   (mode[3:0]==5)    begin out2=0;                           oe2=0;                              end
//	else if   (mode[3:0]==6)	begin out2=0;                           oe2=0; trigin_gpio= gpio_in2;          end
//	else                        begin out2=0;                           oe2=0;                              end
end 

always @( * )
begin
	case(mode[3:0])
	0:		begin	 out3=guide3;                  oe3=1;                         	end 
	1:		begin	 out3=GPSBOX_ShutterMessure;   oe3=1;                           end 
	2:		begin	 out3=slaveXHS;                oe3=1;                           end 
	3:		begin	 out3=0;                       oe3=0;                           end 
	4:		begin	 out3=slaveXHS;                oe3=1;                           end 
	5:		begin	 out3=0;                       oe3=0; HSYNC_SlaveIn=gpio_in3;   end 
	6:		begin	 out3=slaveXHS;                oe3=1;                           end 
	default:begin 	 out3=0;                       oe3=0;                           end 
	endcase
//    if      (mode[3:0]==0)  	     begin out3=guide3;                  oe3=1;                         end
//	else if   (mode[3:0]==1)         begin out3=GPSBOX_ShutterMessure;   oe3=1;                         end 
//	else if   (mode[3:0]==2)         begin out3=slaveXHS;                oe3=1;                         end 
//	else if   (mode[3:0]==3)         begin out3=0;                       oe3=0;                         end 
//	else if   (mode[3:0]==4)         begin out3=slaveXHS;                oe3=1;                         end 
//	else if   (mode[3:0]==5)         begin out3=0;                       oe3=0; HSYNC_SlaveIn=gpio_in3; end 
//	else if   (mode[3:0]==6)		 begin out3=slaveXHS;                oe3=1;                         end 
//	else                             begin out3=0;                       oe3=0;                         end	
end 

always @( * )
begin
	case(mode[3:0])
	0:		begin	 out4=guide4;       oe4=1;                             end 
	1:		begin	 out4=GPSBOX_CLK;   oe4=1;                             end 
	2:		begin	 out4=TrigOut;      oe4=1;                             end 
	3:		begin	 out4=0;            oe4=0;                             end 
	4:		begin	 out4=slaveXVS;     oe4=1;                             end 
	5:		begin	 out4=0;            oe4=0;  VSYNC_SlaveIn=gpio_in4;    end 
	6:		begin	 out4=mode6_led;	oe4=1;                             end 
	default:begin    out4=0;            oe4=0;                             end 
	endcase
//  if        (mode[3:0]==0)  	   begin out4=guide4;       oe4=1;                           end 
//	else if   (mode[3:0]==1)       begin out4=GPSBOX_CLK;   oe4=1;                           end	
//	else if   (mode[3:0]==2)       begin out4=TrigOut;      oe4=1;                           end	
//	else if   (mode[3:0]==3)       begin out4=0;            oe4=0;                           end	
//	else if   (mode[3:0]==4)       begin out4=slaveXVS;     oe4=1;                           end
//	else if   (mode[3:0]==5)       begin out4=0;            oe4=0;  VSYNC_SlaveIn=gpio_in4;  end	
//	else if   (mode[3:0]==6) 	   begin out4=mode6_led;	oe4=1;                           end	
//	else                           begin out4=0;            oe4=0;                           end		
end 


//always @( * )
//begin
//   if        (mode[5:4]==0)  	    begin TrigOut_Opto=!GPSBOX_ShutterMessure;                 end 
//	else if   (mode[5:4]==1)       begin TrigOut_Opto=!TrigOut;                               end
//	else                           begin TrigOut_Opto=0;                                      end		
//end 

endmodule
