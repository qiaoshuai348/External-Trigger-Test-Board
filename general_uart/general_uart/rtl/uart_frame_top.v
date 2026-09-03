

module uart_frame_top
#(
	parameter	CLK_FREQUENCE	= 50_000_000,		//hz
				BAUD_RATE		= 9600		,		//9600��19200 ��38400 ��57600 ��115200��230400��460800��921600
				PARITY			= "NONE"	,		//"NONE","EVEN","ODD"
				FRAME_WD		= 8					//if PARITY="NONE",it can be 5~9;else 5~8
)
(
	input						clk			,	//system_clk
	input						rst_n		,	//system_reset
	input						tx_en		,	//once_tx_start
	input		[FRAME_WD-1:0]	tx_data		,	//data_to_tx
	output		reg				tx_done	=1	,	//once_tx_done
	output						uart_tx		, 	//uart_tx_data
			
	input						uart_rx		,		
	output		[FRAME_WD-1:0]	rx_data 	,		//frame_received,when rx_done = 1 it's valid
	output						rx_done		,		//once_rx_done
	output						rx_error	 		//when the PARITY is enable if frame_error = 1,the frame received is wrong
	
	

);


reg					frame_en	=0	;
reg	[FRAME_WD-1:0]	data_frame	=0	;
wire				tx_done_1		;


always @ (posedge clk )begin
	if( tx_en )begin 
		data_frame <=  tx_data ;
		frame_en   <=  1;
	end else begin 
		data_frame <=  data_frame ;
		frame_en   <= 0;
	end 
end 
//   frame_en     data_frame

always @ (posedge clk )begin
	if(tx_en)begin
	       tx_done <= 0;
	end else if ( tx_done_1  )begin 
		   tx_done <= 1;
    end else begin 
    	   tx_done <= tx_done;
    end 
end 
//tx_done

 uart_frame_tx
#(
	.CLK_FREQUENCE	(CLK_FREQUENCE	),		//hz
	.BAUD_RATE		(BAUD_RATE		),		//9600��19200 ��38400 ��57600 ��115200��230400��460800��921600
	.PARITY			(PARITY			),		//"NONE","EVEN","ODD"
	.FRAME_WD		(FRAME_WD		)		//if PARITY="NONE",it can be 5~9;else 5~8
)  uart_frame_tx_inst                                        
(                                          
	.clk			(clk			),		//system_clk
	.rst_n			(rst_n			),		//system_reset
	.frame_en		(frame_en		),		//once_tx_start
	.data_frame		(data_frame		),		//data_to_tx
	.tx_done		(tx_done_1		),		//once_tx_done
	.uart_tx		(uart_tx		) 		//uart_tx_data
);


 uart_frame_rx
#(
	.CLK_FREQUENCE	(CLK_FREQUENCE	),		//hz
	.BAUD_RATE		(BAUD_RATE		),		//9600��19200 ��38400 ��57600 ��115200��230400��460800��921600
	.PARITY			(PARITY			),		//"NONE","EVEN","ODD"
	.FRAME_WD		(FRAME_WD		)		//if PARITY="NONE",it can be 5~9;else 5~8
)  uart_frame_rx_inst                                   
(                                       
	.clk			(clk			),		//sys_clk
	.rst_n			(rst_n			),		
	.uart_rx		(uart_rx		),		
	.rx_frame		(rx_data		),		//frame_received,when rx_done = 1 it's valid
	.rx_done		(rx_done		),		//once_rx_done
	.frame_error	(rx_error		) 		//when the PARITY is enable if frame_error = 1,the frame received is wrong
);




endmodule