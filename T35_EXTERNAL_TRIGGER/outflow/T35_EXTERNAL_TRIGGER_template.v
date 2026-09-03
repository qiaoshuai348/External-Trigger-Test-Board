
// Efinity Top-level template
// Version: 2021.2.323.2.18
// Date: 2026-09-01 16:26

// Copyright (C) 2017 - 2021 Efinix Inc. All rights reserved.

// This file may be used as a starting point for Efinity synthesis top-level target.
// The port list here matches what is expected by Efinity constraint files generated
// by the Efinity Interface Designer.

// To use this:
//     #1)  Save this file with a different name to a different directory, where source files are kept.
//              Example: you may wish to save as C:\T35_ET\T35_EXTERNAL_TRIGGER.v
//     #2)  Add the newly saved file into Efinity project as design file
//     #3)  Edit the top level entity in Efinity project to:  T35_EXTERNAL_TRIGGER
//     #4)  Insert design content.


module T35_EXTERNAL_TRIGGER
(
  input syspll_LOCKED,
  input MUX_GPIO1_IN,
  input MUX_GPIO3_IN,
  input sysclk,
  input syspll_CLKOUT100,
  output MUX_GPIO2_OUT,
  output MUX_GPIO4_OUT
);


endmodule

