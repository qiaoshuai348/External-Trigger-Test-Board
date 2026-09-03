`timescale 1ns / 1ps
//-------------------------------------------------------------------------------
// Company:  QHYCCD
// Engineer: YangSK
// 
// Create Date: 2022/4/6
// Design Name: T35_TOP
// Module Name: unsigned_reg_mult
// Project Name: T35_FRAMEWORK
// Target Devices: t35f324
// Tool Versions: EFINITY21.2
// Description: data Delay two beats
// Dependencies: 
// 
// Revision:rev1 
// 
// Additional Comments:
// 
//--------------------------------------------------------------------------------

module unsigned_reg_mult
#(
	parameter WIDTHA=16								,
	parameter WIDTHB=8
)
(
   input 							clk				,
   input 		[WIDTHA-1:0] 		a				,
   input 		[WIDTHB-1:0] 		b				,
   output wire 	[WIDTHB+WIDTHA-1:0] o 
);

//   reg [WIDTHA-1:0] a_reg=0;
//   reg [WIDTHB-1:0] b_reg=0;
//   wire [WIDTHB+WIDTHA-1:0] out;
//
//   assign out = a_reg * b_reg;
//
//   always @ (posedge clk)
//      begin
//         a_reg <= a;
//	 b_reg <= b;
//	 o <= out;
//     end





  
EFX_MULT # (

	.WIDTH			(18			),
	.A_REG			(1			),
	.B_REG			(1			),
	.O_REG			(1			),
	.CLK_POLARITY	(1'b1		), // 0 falling edge, 1 rising edge
	.CEA_POLARITY	(1'b1		), // 0 falling edge, 1 rising edge
	.RSTA_POLARITY	(1'b1		), // 0 falling edge, 1 rising edge
	.RSTA_SYNC		(1'b0		), // 0 aynchronous, 1 synchronous
	.RSTA_VALUE		(1'b1		), // 0 reset, 1 set
	.CEB_POLARITY	(1'b1		), // 0 falling edge, 1 rising edge
	.RSTB_POLARITY	(1'b1		), // 0 falling edge, 1 rising edge
	.RSTB_SYNC		(1'b0		), // 0 aynchronous, 1 synchronous
	.RSTB_VALUE		(1'b1		), // 0 reset, 1 set
	.CEO_POLARITY	(1'b1		), // 0 falling edge, 1 rising edge
	.RSTO_POLARITY	(1'b1		), // 0 falling edge, 1 rising edge
	.RSTO_SYNC		(1'b0		), // 0 aynchronous, 1 synchronous
	.RSTO_VALUE		(1'b1		) // 0 reset, 1 set
	
) mult (

	.CLK			(clk		),
	.CEA			(1'b1		),
	.RSTA			(1'b0		),
	.CEB			(1'b1		),
	.RSTB			(1'b0		),
	.CEO			(1'b1		),
	.RSTO			(1'b0		),
	.A				({'d0,a}	),//input [17:0]
	.B				({'d0,b}	),//input [17:0]
	.O				(o			) //output[35:0]
	
);



endmodule
