using System;

// Space Engineers DLLs
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using Sandbox.Game.EntityComponents;
using VRageMath;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Reflection;
using Sandbox.ModAPI.Interfaces.Terminal;
using System.Xml.Schema;

namespace ImprovedBetterPowerScript {


public sealed class Program : MyGridProgram {


    #region ImprovedBetterPowerScript

    /*
    MIT License

    Copyright (c) 2025 Blayy

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in all
    copies or substantial portions of the Software.

    **THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    SOFTWARE.**
    */
//------------------------------------------------------------------------------------- {LICENSE}
    
    bool gridHasPowerProduction = true;
    bool gridHasPowerStorage = true;
    bool ignoreLCD = false;
    const string outputLcdName = "MB PowerDisplay";
//------------------------------------------------------------------------------------- {BEHAVIOURS}
    
    List<IMyPowerProducer> powerProducers = new List<IMyPowerProducer>();
    List<IMyBatteryBlock> powerStorage = new List<IMyBatteryBlock>();
    List<IMyTerminalBlock> gridBlocks = new List<IMyTerminalBlock>();
    List<string> subGridNames = new List<string>();
    IMyTextSurface terminalLCD;
    IMyTextPanel displayLCD;
//------------------------------------------------------------------------------------- {OBJECTS}
   
   bool runScript = true;
    bool producerExists = false;
    bool storageExists = false;
    bool consumersExists = false;
    string debugOutput;
//------------------------------------------------------------------------------------- {VARIABLES}

    void writeOutput(string text) 
    {   
        if (!ignoreLCD)
        {
            if (displayLCD != null) // Checks if the LCD exists
            {
                if (displayLCD.IsWorking) // Checks if LCD is working
                {
                    displayLCD.WriteText(text, false);
                    terminalLCD.WriteText(debugOutput,false);
                }
                else 
                {
                debugOutput += $"LCD: '{outputLcdName}' not working.\n";
                terminalLCD.WriteText(text, false);
                }
            }
            else 
            {
                terminalLCD.WriteText(text,false);
                debugOutput += $"LCD: '{outputLcdName}' not found.\n";   
            } 
        }
        else
        {
          terminalLCD.WriteText(text, false);  
        }
        
        // Always show debug information in terminal
        Echo(debugOutput);
    }
    public string determineUnit(double powerValue)
    {
        if (powerValue > 0)
        {
            string[] units = { "W", "kW", "MW", "GW" };
            int UnitPlacement = 2;
        
            while(powerValue < 1 || powerValue >= 1000)
            {
                if (powerValue < 1)
                {
                    powerValue *= 1000;
                    UnitPlacement--;
                }
            
                if (powerValue >= 1000)
                {
                    powerValue /= 1000;
                    UnitPlacement++;    
                }
            }

            return $"{powerValue} {units[UnitPlacement]}";
        }
        else
        {
            return "0 MW";
        }  
    }
//------------------------------------------------------------------------------------- {METHODES}

    public Program() // Constructor
        {
            // Runs every 1.6s
            Runtime.UpdateFrequency = UpdateFrequency.Update100;

            // Programmable Block (Defualt Output)
            terminalLCD = Me.GetSurface(0);
            writeOutput("Initializing Power Grid Script...\n\n");

            // Try get needed objects
            displayLCD = GridTerminalSystem.GetBlockWithName(outputLcdName) as IMyTextPanel;

            /*
            Adds all blocks in a list in their own construct/grid
            */

            GridTerminalSystem.GetBlocks(gridBlocks);
            foreach (IMyTerminalBlock block in gridBlocks)
            {
                if (block.IsSameConstructAs(Me)) // Checks if block is part of the construct
                {

                    if (block is IMyPowerProducer) // Checks for type Producer
                    {

                        if (block is IMyBatteryBlock) // Checks for type Battery
                        {
                            powerStorage.Add(block as IMyBatteryBlock);
                            writeOutput($"Found Battery: '{block.CustomName}'\n");
                        }
                        else
                        {
                            powerProducers.Add(block as IMyPowerProducer);
                            writeOutput($"Found Producer: '{block.CustomName}'\n");
                        }
                    }
                }
                else
                {
                    // {OTHER GRIDS}
                }
            }

            /*
                Setting checks for debugging:
                    Has a working output LCD?
                    *Has al least one output is selected?
                    *Any Power Producers?
                    *Any Power Storage?
                    *Any Power Consumers?
                     
            */

            // Checks if there is a working displayLCD
            if (displayLCD != null)
            {
                if (displayLCD.IsWorking)
                {
                    writeOutput($"Found LCD: '{outputLcdName}'\n");
                }
                else
                {
                    writeOutput($"{outputLcdName} not working\n");
                    writeOutput("Reverting to defualt LCD\n");
                }
            }
            else
            {
                writeOutput($"LCD: '{outputLcdName}' not found\n");
            }

            // Checks for Power Producers.
            if (powerProducers.Count != 0)
            {
                producerExists = true;
                writeOutput("Listed Producers succesfully\n");
            }
            else
            {
                writeOutput("No Producers found\n");
            }

            // Checks for Power Storage. 
            if (powerStorage.Count != 0)
            {
                storageExists = true;
                writeOutput("Listed Storage succesfully\n");
            }
            else
            {
                writeOutput("No Storage found\n");
            }

            if (!producerExists && !storageExists && !consumersExists)
            {
                runScript = false;
            }

            // Checks if user is dumb.
            if (!gridHasPowerProduction && !gridHasPowerStorage)
            {
                runScript = false;
            }

            // Script will not run
            if (!runScript)
            {
                writeOutput("Initialization failed...\n");
                writeOutput(debugOutput);
                return;
            }
            else
            {
                writeOutput("Script successfully Initialized");
            }
        }
    
    public void Save() // Save state
    { 
      if (!runScript) // Won't run the script if the requirments are not met.
        {
            return;
        }      
    }

    public void Main(string argument, UpdateType updateSource) // Running code
    {
        if (!runScript) // Won't run the script if the requirments are not met.
        {
            return;
        }
        
        string output = "Better Power Grid for Space Engineers:\n" +
                        "Updates every 1.6s\n\n";
        debugOutput = "Debug Information (Updates every 1.6s) :\n\n";
        
        string producersOutput = "";
        string storageOutput = "";

        debugOutput = "Debug Information (Updates every 1.6s):\n\n";

        if (gridHasPowerProduction)
        {
            // Producer's outputs
            float currentPowerProduced = 0f;
            float maxProducersOutput = 0f;

            // Loops through all the producers in the list
            foreach (IMyPowerProducer producer in powerProducers)
            {
                if (producer.IsWorking) // Checks if the producer is working
                {
                    currentPowerProduced += producer.CurrentOutput;
                    maxProducersOutput += producer.MaxOutput;
                }
                else // if not working add to debug
                {
                    debugOutput += $"'{producer.CustomName}' is not working.\n";
                }
            }
        
            double productionEfficiency = 0d;
    
            if (maxProducersOutput != 0) // Can't devide with 0.
            {
                // current/max * 100 gives %
                productionEfficiency = Math.Round(currentPowerProduced/maxProducersOutput*100,2);
            }

            // le output of current producer's information
            producersOutput =  "Power Production:\n"+
                                $"Power produced: {determineUnit(currentPowerProduced)}h\n" + 
                                $"Max production: {determineUnit(maxProducersOutput)}h\n"+
                                $"Production Efficiency: {productionEfficiency}%";
        }
        
        if (gridHasPowerStorage) // Grid has power Storage flag.
        {
            // Storage's outputs
            float currentPowerStored = 0f;
            float maxPowerStorage = 0f;
            float currentStorageOutput = 0f;

            foreach (IMyBatteryBlock storage in powerStorage)
            {
                if (storage.IsWorking) // Checks if the battery is working
                {
                    currentPowerStored += storage.CurrentStoredPower;
                    maxPowerStorage += storage.MaxStoredPower;
                    currentStorageOutput += storage.CurrentOutput; 
                }
                else // if not working add to debug
                {
                    debugOutput += $"'{storage.CustomName}' is not working.\n";
                }
            }

            double storageEfficiency = 0d;
            float timeLeft = 0f;
            string powerLeft = $"{timeLeft:F2} s";

            if (maxPowerStorage != 0)
            {
                storageEfficiency = Math.Round(currentPowerStored/maxPowerStorage*100,2);
                
                timeLeft = currentPowerStored/currentStorageOutput ; // Gets in hours 
                if(timeLeft <= 1 )
                {
                    timeLeft *= 60; // to mins
                    powerLeft = $"{timeLeft:F2} mins";
                
                    if (timeLeft <= 60)
                    {
                        timeLeft *= 60; // To seconds
                        powerLeft = $"{timeLeft:F2} s";
                    }
                }
                else
                {   
                    if (timeLeft != float.PositiveInfinity)
                    {
                        powerLeft = $"{timeLeft:F2} hours";
                    }
                    else
                    {
                        powerLeft = "Infinity";
                    } 
                }   
            }
            // Power storage output
            storageOutput = "Power Storage:\n" +
                            $"Power Stored: {powerStorage}\n" +
                            $"Max Storage: {determineUnit(maxPowerStorage)}\n" +
                            $"Storage at: {storageEfficiency}%\n" +
                            $"Time before depleted: {powerLeft}";
        }
        
        // What the output should be depending on the user's needs. 
        if(gridHasPowerProduction && gridHasPowerStorage) // Show both
        {
            output += producersOutput +"\n\n"+ storageOutput;
        }
        else if (!gridHasPowerProduction) // Only power Storage
        {
            output += storageOutput;
        }
        else if (!gridHasPowerStorage) // Only Power production
        {
            output += producersOutput;
        }
        
        output += "\n\nDebug information sent to console.";
        
         
    }

    #endregion // ImprovedBetterPowerScript
}}