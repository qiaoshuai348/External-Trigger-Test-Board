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
COMPONENT asmi_flash_ctl is
PORT (
rst_in : in std_logic;
clk_in : in std_logic;
fast_read : in std_logic;
sector_erase : in std_logic;
page_write : in std_logic;
fast_read_dual : in std_logic;
quad_fast_read : in std_logic;
quad_io_fast_read : in std_logic;
quad_page_write : in std_logic;
quad_enable : in std_logic;
rden : in std_logic;
wren : in std_logic;
shift_bytes : in std_logic;
datain : in std_logic_vector(7 downto 0);
dataout : out std_logic_vector(7 downto 0);
data_valid : out std_logic;
busy : out std_logic;
miso : in std_logic;
miso_1 : in std_logic;
miso_2 : in std_logic;
miso_3 : in std_logic;
sclk : out std_logic;
nss : out std_logic;
mosi : out std_logic;
mosi_1 : out std_logic;
mosi_2 : out std_logic;
mosi_3 : out std_logic;
mosi_oe : out std_logic;
address : in std_logic_vector(23 downto 0));
END COMPONENT;
---------------------- End COMPONENT Declaration ------------

------------- Begin Cut here for INSTANTIATION Template -----
u_asmi_flash_ctl : asmi_flash_ctl
PORT MAP (
rst_in => rst_in,
clk_in => clk_in,
fast_read => fast_read,
sector_erase => sector_erase,
page_write => page_write,
fast_read_dual => fast_read_dual,
quad_fast_read => quad_fast_read,
quad_io_fast_read => quad_io_fast_read,
quad_page_write => quad_page_write,
quad_enable => quad_enable,
rden => rden,
wren => wren,
shift_bytes => shift_bytes,
datain => datain,
dataout => dataout,
data_valid => data_valid,
busy => busy,
miso => miso,
miso_1 => miso_1,
miso_2 => miso_2,
miso_3 => miso_3,
sclk => sclk,
nss => nss,
mosi => mosi,
mosi_1 => mosi_1,
mosi_2 => mosi_2,
mosi_3 => mosi_3,
mosi_oe => mosi_oe,
address => address);
------------------------ End INSTANTIATION Template ---------
