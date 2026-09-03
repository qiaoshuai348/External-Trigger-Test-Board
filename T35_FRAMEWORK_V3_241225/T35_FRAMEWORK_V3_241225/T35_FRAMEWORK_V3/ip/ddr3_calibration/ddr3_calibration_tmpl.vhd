////////////////////////////////////////////////////////////////////////////////
// Copyright (C) 2013-2021 Efinix Inc. All rights reserved.              
//
// This   document  contains  proprietary information  which   is        
// protected by  copyright. All rights  are reserved.  This notice       
// refers to original work by Efinix, Inc. which may be derivitive       
// of other work distributed under license of the authors.  In the       
// case of derivative work, nothing in this notice overrides the         
// original author's license agreement.  Where applicable, the           
// original license agreement is included in it's original               
// unmodified form immediately below this header.                        
//                                                                       
// WARRANTY DISCLAIMER.                                                  
//     THE  DESIGN, CODE, OR INFORMATION ARE PROVIDED “AS IS” AND        
//     EFINIX MAKES NO WARRANTIES, EXPRESS OR IMPLIED WITH               
//     RESPECT THERETO, AND EXPRESSLY DISCLAIMS ANY IMPLIED WARRANTIES,  
//     INCLUDING, WITHOUT LIMITATION, THE IMPLIED WARRANTIES OF          
//     MERCHANTABILITY, NON-INFRINGEMENT AND FITNESS FOR A PARTICULAR    
//     PURPOSE.  SOME STATES DO NOT ALLOW EXCLUSIONS OF AN IMPLIED       
//     WARRANTY, SO THIS DISCLAIMER MAY NOT APPLY TO LICENSEE.           
//                                                                       
// LIMITATION OF LIABILITY.                                              
//     NOTWITHSTANDING ANYTHING TO THE CONTRARY, EXCEPT FOR BODILY       
//     INJURY, EFINIX SHALL NOT BE LIABLE WITH RESPECT TO ANY SUBJECT    
//     MATTER OF THIS AGREEMENT UNDER TORT, CONTRACT, STRICT LIABILITY   
//     OR ANY OTHER LEGAL OR EQUITABLE THEORY (I) FOR ANY INDIRECT,      
//     SPECIAL, INCIDENTAL, EXEMPLARY OR CONSEQUENTIAL DAMAGES OF ANY    
//     CHARACTER INCLUDING, WITHOUT LIMITATION, DAMAGES FOR LOSS OF      
//     GOODWILL, DATA OR PROFIT, WORK STOPPAGE, OR COMPUTER FAILURE OR   
//     MALFUNCTION, OR IN ANY EVENT (II) FOR ANY AMOUNT IN EXCESS, IN    
//     THE AGGREGATE, OF THE FEE PAID BY LICENSEE TO EFINIX HEREUNDER    
//     (OR, IF THE FEE HAS BEEN WAIVED, $100), EVEN IF EFINIX SHALL HAVE 
//     BEEN INFORMED OF THE POSSIBILITY OF SUCH DAMAGES.  SOME STATES DO 
//     NOT ALLOW THE EXCLUSION OR LIMITATION OF INCIDENTAL OR            
//     CONSEQUENTIAL DAMAGES, SO THIS LIMITATION AND EXCLUSION MAY NOT   
//     APPLY TO LICENSEE.                                                
//
////////////////////////////////////////////////////////////////////////////////
------------- Begin Cut here for COMPONENT Declaration ------
COMPONENT ddr3_calibration is
PORT (
rst_n : in std_logic;
clk : in std_logic;
ddr_cal_auto_en : in std_logic;
ddr_cal_auto_sel : in std_logic_vector(3 downto 0);
pll_locked : in std_logic;
pll_ddr_locked : in std_logic;
pll_rst_n : out std_logic;
pll_ddr_rst_n : out std_logic;
ddr_cal_auto_status : out std_logic_vector(7 downto 0);
auto_cal_done : out std_logic;
auto_cal_err : out std_logic;
ddr_slave_i2c_scl : out std_logic;
ddr_slave_i2c_sda : out std_logic;
ddr_slave_i2c_sda_in : in std_logic);
END COMPONENT;
---------------------- End COMPONENT Declaration ------------

------------- Begin Cut here for INSTANTIATION Template -----
u_ddr3_calibration : ddr3_calibration
PORT MAP (
rst_n => rst_n,
clk => clk,
ddr_cal_auto_en => ddr_cal_auto_en,
ddr_cal_auto_sel => ddr_cal_auto_sel,
pll_locked => pll_locked,
pll_ddr_locked => pll_ddr_locked,
pll_rst_n => pll_rst_n,
pll_ddr_rst_n => pll_ddr_rst_n,
ddr_cal_auto_status => ddr_cal_auto_status,
auto_cal_done => auto_cal_done,
auto_cal_err => auto_cal_err,
ddr_slave_i2c_scl => ddr_slave_i2c_scl,
ddr_slave_i2c_sda => ddr_slave_i2c_sda,
ddr_slave_i2c_sda_in => ddr_slave_i2c_sda_in);
------------------------ End INSTANTIATION Template ---------
