############## this is all setup, go to next cell #####################
#!pip install nidaqmx
#!pip install matplotlib
#!pip install scipy

import nidaqmx
from nidaqmx.constants import LineGrouping
from nidaqmx.constants import AcquisitionType 
from nidaqmx.stream_readers import AnalogMultiChannelReader,AnalogSingleChannelReader
from nidaqmx.constants import WAIT_INFINITELY
from scipy.signal import savgol_filter
from scipy import signal
import json
import time
import os
import numpy as np
import matplotlib.pyplot as plt
from datetime import datetime
import requests
import urllib3
urllib3.disable_warnings(urllib3.exceptions.InsecureRequestWarning)

class DempBot:
    def __init__(self, settingsFolder, dataFolder, quickGraphs=None):
        self.settingsFolder = settingsFolder
        self.dataFolder = dataFolder
        self.quickGraphs=quickGraphs
        self.zeroBias =0
        if quickGraphs!=None:
            quickGraphs.DeleteAll()

        with  open(settingsFolder,'r') as f: 
            self.channels = json.load(f)

        self.BiasChannel_AO = "cDAQ1Mod4/ao0"
        self.RefChannel_AO =  "cDAQ1Mod4/ao1"
        
        self.BiasChannel_AI = "cDAQ1Mod1/ai0"
        self.RefChannel_AI =  "cDAQ1Mod1/ai1"
        
        self.CurrentChannels_AI = []
        self.sampleRate =50000
        self.startTime = time.time()
        for channel in self.channels:
            if self.channels[channel]['ChannelFunction']==0:
                self.CurrentChannels_AI.append(self.channels[channel])
                sampleRate = self.channels[channel]['SampleRate']
                if sampleRate>self.sampleRate:
                    self.sampleRate = sampleRate

    def activeSleep(self,seconds):
        start = time.time()
        while (time.time() - start) < (seconds):
            time.sleep(0.001)  # Sleep for a short time to avoid busy waiting
            pass
            
    def _Waveform_Read(self, waveform , outSampleRate=-1, inSampleRate =-1,  plot=False, activeChannels=[]):
        
        if plot==True and self.quickGraphs==None:
            raise Exception("You must have graph module loaded to plot")
            
        if inSampleRate ==-1:
            inSampleRate = self.sampleRate
        if outSampleRate ==-1:
            outSampleRate = inSampleRate

        
        measureTime_S= len(waveform)/ outSampleRate
        totalPoints = int(inSampleRate* measureTime_S   ) 
        

        segmentPoints = np.min([inSampleRate, totalPoints])
        usedChannels =[]
        nChannels=0
        with nidaqmx.Task(new_task_name ='Current_MonTask') as Current_MonTask, nidaqmx.Task(new_task_name ='biasTask') as biasTask:
            if activeChannels==None or len(activeChannels)==0 :
                for channel in self.CurrentChannels_AI:
                    Current_MonTask.ai_channels.add_ai_voltage_chan(channel['Device_Handle'])
                    usedChannels.append( channel['Name'])
                    nChannels+=1
            else:
                for channel in self.CurrentChannels_AI:
                    name = channel['Name']
                    if name in activeChannels:
                        Current_MonTask.ai_channels.add_ai_voltage_chan(channel['Device_Handle'])
                        usedChannels.append(name )
                        nChannels+=1

            Current_MonTask.ai_channels.add_ai_voltage_chan(self.BiasChannel_AI)
            nChannels+=1
    
            biasTask.ao_channels.add_ao_voltage_chan(self.BiasChannel_AO)
            Current_MonTask.timing.cfg_samp_clk_timing( rate=inSampleRate , source="OnboardClock", sample_mode = AcquisitionType.FINITE, samps_per_chan=totalPoints)
            biasTask       .timing.cfg_samp_clk_timing( rate=outSampleRate, sample_mode= AcquisitionType.FINITE, samps_per_chan= len(waveform))

            reader = AnalogMultiChannelReader(Current_MonTask.in_stream)
            biasTask.write( waveform+self.zeroBias , auto_start=False)

            captured=[]
            pointsRead=0
            times=[]
            start_time = time.time()

            
            while pointsRead<totalPoints:
                currents = np.zeros([nChannels, totalPoints])
                if pointsRead==0:
                    Current_MonTask.start()
                    biasTask.start()
                #currents = Current_MonTask.read(segmentPoints, timeout=WAIT_INFINITELY) 
                reader.read_many_sample(data = currents, number_of_samples_per_channel = totalPoints,timeout=WAIT_INFINITELY )

                
                captured .append(currents)
                pointsRead+=totalPoints
                
                print('.',end='' )
                if plot==True:
                    x=currents[-1][::10]
                    for channelIndex in range(len(usedChannels)):
                        self._Graphs[usedChannels[channelIndex]].StreamScatter(x,np.array(currents[channelIndex][::100])*-2)
                         
                    #for channelIndex in range(len(self.CurrentChannels_AI)):
                    #    self._Graphs[self.CurrentChannels_AI[channelIndex]['Name']].StreamScatter(x,np.array(currents[channelIndex][::10])*-2)
                        
            
        end_time = time.time()

        outChannels = {}
        outChannels ["Bias"] = []
        for channelIndex in range(len(usedChannels)):
            name = usedChannels[channelIndex] 
            outChannels[name]=[]
        
        for channelIndex in range(len(usedChannels)):
            name = usedChannels[channelIndex] 
            for capture in captured:
                outChannels[name].extend( capture[channelIndex])
                
        for capture in captured:
                outChannels["Bias"].extend( capture[-1])

        channelCut = int(len(np.array(outChannels['Bias']))*.065)
        for channel in outChannels:
            if channel!='Bias' and channel!='Time':
                data =np.array(outChannels[channel])*-2
                outChannels[channel]=   data[ channelCut:]
            else:
                data= np.array(outChannels[channel])
                outChannels[channel]=  data[ channelCut:]
        return outChannels,inSampleRate

    def _CreateTrianglWave(self,amplitude, sweeprate, sampleRate):
        cycletime = 4*amplitude/sweeprate
       
        voltages = np.zeros( int(cycletime*sampleRate), dtype=float)
        times = np.zeros_like(voltages)
        bias =0
        time =0
        sampleTime =1/sampleRate
        direction = sweeprate*sampleTime
        for i in range(len(voltages)):
            voltages[i]=bias
            times[i]=time
            bias +=direction
            time+=sampleTime
            if np.abs(bias)>amplitude:
                direction=-1*direction
        return times,voltages
    
    def _CreateRTGraphs(self, activeChannels, xlabel="Time(s)"):
        self.quickGraphs.DeleteAll()
        self._Graphs ={}
        if activeChannels==None or len(activeChannels)==0:
            for channelIndex in range(len(self.CurrentChannels_AI)):
                name = self.CurrentChannels_AI[channelIndex]['Name']
                self._Graphs[name]=self.quickGraphs.CreateDataStreamGraph(name, xlabel, "Current (nA)")
        else:
            for name in activeChannels:
                self._Graphs[name]=self.quickGraphs.CreateDataStreamGraph(name, xlabel, "Current (nA)")

    def _CreateSignalGraphs(self, activeChannels, xlabel="Time(s)"):
        self.quickGraphs.DeleteAll()
        self._Graphs ={}
        if activeChannels==None or len(activeChannels)==0:
            for channelIndex in range(len(self.CurrentChannels_AI)):
                name = self.CurrentChannels_AI[channelIndex]['Name']
                self._Graphs[name]=self.quickGraphs.CreateSignalGraph(name, xlabel, "Current (nA)")
        else:
            for name in activeChannels:
                self._Graphs[name]=self.quickGraphs.CreateSignalGraph(name, xlabel, "Current (nA)")

    def _CreateNultiGraphs(self, activeChannels, xlabel="Time(s)"):
        self.quickGraphs.DeleteAll()
        self._Graphs ={}
        if activeChannels==None or len(activeChannels)==0:
            for channelIndex in range(len(self.CurrentChannels_AI)):
                name = self.CurrentChannels_AI[channelIndex]['Name']
                self._Graphs[name]=self.quickGraphs.CreateMultiScatterGraph(name, xlabel, "Current (nA)")
        else:
            for name in activeChannels:
                self._Graphs[name]=self.quickGraphs.CreateMultiScatterGraph(name, xlabel, "Current (nA)")
                
    def _CreateIVGraphs(self, activeChannels, xlabel="Voltage(V)"):
        self.quickGraphs.DeleteAll()
        self._Graphs ={}
        if activeChannels==None or len(activeChannels)==0:
            for channelIndex in range(len(self.CurrentChannels_AI)):
                name = self.CurrentChannels_AI[channelIndex]['Name']
                self._Graphs[name]=self.quickGraphs.CreateScatterGraph(name, xlabel, "Current (nA)")
        else:
            for name in activeChannels:
                self._Graphs[name]=self.quickGraphs.CreateScatterGraph(name, xlabel, "Current (nA)")

    def Zero(self):
         
        self.Bias( 0 )

    def FixOffset(self,resistorChannel):
        bias = np.linspace(-.02,.02,15000)
        self.zeroBias=0
        self.Zero()
        currents=self._Waveform_Read (bias, activeChannels=[resistorChannel])[0]
        self.Zero()

        recordedCurrents=currents[resistorChannel]*-1
        if np.min(recordedCurrents)>0:
            p=np.polyfit(currents[resistorChannel],currents['Bias'],1)
            self.zeroBias = np.polyval(p,[0])*-1
        else:
            for i in range(len(recordedCurrents)-1):
                if recordedCurrents[i]>=0 and recordedCurrents[i+1]<=0:
                    self.zeroBias = (currents['Bias'][i] +currents['Bias'][i+1])/-2
        
        print("Offset Bias:",self.zeroBias)
        self.Zero()
        currents=self._Waveform_Read (bias, activeChannels=[resistorChannel])[0]
        self.Zero()
        return currents 
        
    


    def Bias(self,bias):
        with nidaqmx.Task(new_task_name ='biasTask') as biasTask:
            biasTask.ao_channels.add_ao_voltage_chan(self.BiasChannel_AO)
            biasTask.write([bias+self.zeroBias ] , auto_start=True)
            biasTask.start()    

    def Stream_data(self, channelData):
        for channel in channelData:
            if channel!='Time' and channel!='Bias':
                data = np.array(channelData[channel])
                print(data)
                self._Graphs[channel].StreamData(data)

    def Stream_scatter(self, channelData):
        bias = channelData['Bias']
        for channel in channelData:
            if channel!='Time' and channel!='Bias':
                self._Graphs[channel].StreamScatter(bias,channelData[channel])

    def _Constant_Read(self,  measureTime_S ,bias, sampleRate=-1 , plot=False, activeChannels=[], includeBias=False):

        if plot==True and self.quickGraphs==None:
            raise Exception("You must have graph module loaded to plot")

       
        if sampleRate ==-1:
            sampleRate = self.sampleRate
            
        totalPoints = int(sampleRate* measureTime_S   ) 

        segmentPoints = np.min([sampleRate, totalPoints])

        usedChannels =[]
        with nidaqmx.Task(new_task_name ='Current_MonTask') as Current_MonTask, nidaqmx.Task(new_task_name ='biasTask') as biasTask:
            if activeChannels==None or len(activeChannels)==0 :
                for channel in self.CurrentChannels_AI:
                    Current_MonTask.ai_channels.add_ai_voltage_chan(channel['Device_Handle'])
                    usedChannels.append( channel['Name'])
            else:
                for channel in self.CurrentChannels_AI:
                    name = channel['Name']
                    if name in activeChannels:
                        Current_MonTask.ai_channels.add_ai_voltage_chan(channel['Device_Handle'])
                        usedChannels.append(name )

             
    
            biasTask.ao_channels.add_ao_voltage_chan(self.BiasChannel_AO)
            Current_MonTask.timing.cfg_samp_clk_timing( rate=sampleRate, source="OnboardClock", samps_per_chan=segmentPoints)
    
            biasTask.write([bias+self.zeroBias ] , auto_start=True)
            biasTask.start()

            captured=[]
            pointsRead=0
            times=[]
            start_time = time.time()
            while pointsRead<totalPoints:
                currents = Current_MonTask.read(segmentPoints, timeout=WAIT_INFINITELY)
                pointsRead+=segmentPoints
                captured .append(currents)
                print('.',end='' )
                if plot==True:
                    for channelIndex in range(len(usedChannels)):
                        self._Graphs[usedChannels[channelIndex]].StreamData(np.array(currents[channelIndex][::100])*-2)

            
        end_time = time.time()
        
        outChannels = {}
        outChannels ["Time"] = []
        for channel  in  usedChannels:
            outChannels[channel]=[]
        
        for channelIndex in range(len(usedChannels)):
            name =usedChannels[channelIndex]
            for capture in captured:
                outChannels[name].extend( capture[channelIndex])

        for channel in outChannels:
            if channel!='Time' and channel!='Bias':
                outChannels[channel]=np.array( outChannels[channel])*-2
            else:
                outChannels[channel]=np.array( outChannels[channel])
            
        outChannels["Time"] = np.linspace(0, len(outChannels[name]    )/sampleRate,len(outChannels[name]))
            
        return outChannels,sampleRate

    def Experiment(self, wafer, chip, notes, tags):
        self.wafer = wafer.strip().upper()
        self.chip = chip.strip().upper()
        self.notes = notes.strip()
        self.tags =[x.strip() for x in tags.split(",")]
        if os.path.exists(self.dataFolder + "\\"  + self.wafer)==False:
            os.mkdir(self.dataFolder + "\\"  + self.wafer)
        if os.path.exists(self.dataFolder + "\\"  + self.wafer + "\\" + self.chip)==False:
            os.mkdir(self.dataFolder + "\\"  + self.wafer + "\\" + self.chip)
        self.experimentFolder = self.dataFolder + "\\"  + self.wafer + "\\" + self.chip
        self.experimentInfo = { "wafer":self.wafer,"chip":self.chip,"notes":self.notes,"tags":self.tags,"startTime": datetime.now().strftime('%Y-%m-%d %H_%M_%S')}
        # now write output to a file
        with open(self.experimentFolder + "\\experimentInfo.json", "w") as f:
            # magic happens here to make it pretty-printed
            f.write(
                json.dumps( self.experimentInfo, indent=4, sort_keys=True)
            )
            
        self.quickGraphs.DeleteAllLong()
        self._LongGraphs ={}
        
        self._LongGraphs['low']=self.quickGraphs.CreateLongGraph('Low Traces', "Time(hr)", "Current (nA)")
        self._LongGraphs['high']=self.quickGraphs.CreateLongGraph('High Traces', "Time(hr)", "Current (nA)")
        self.startTime = time.time()

 

    def _LongGraphPoint_RT(self, analyte,sampleRate, outChannels ):
        
        timesL=[  ]
        meansL=[]
        maxsL =[]
        minsL =[]
        stdsL =[]

        timesH=[  ]
        meansH=[]
        maxsH =[]
        minsH =[]
        stdsH =[]
        gaps = 60*sampleRate
        timeC = outChannels['Time']
        baseTime=(time.time()-self.startTime - len(timeC)/sampleRate)
        if gaps<len(timeC):
            endF = len(timeC)-gaps
            skips = gaps
        else :
            endF = 1
            skips = 1
            gaps = len(timeC)-1
            
        for i in range(0,endF,skips):
            sI=i
            eI=np.min([i+gaps,len(timeC)-1])
            mL = []
            mH = []
            for channel in outChannels:
                cut = outChannels[channel][sI:eI]
                mean= np.abs(np.mean( cut ))
                if mean<1:
                    mL .append(mean)
                else:
                    max= np.max(cut)
                    if max<13.8:
                        mH .append(mean)
                        
            if len(mL)>0:
                mean = np.mean(mL)
                timesL.append((baseTime+sI/sampleRate)/60.0/60)
                meansL.append(mean)
                maxsL.append(np.max(mL))
                minsL.append(np.min(mL))
                stdsL.append(np.std(mL)+mean)
                
            if len(mH)>0:
                timesH.append((baseTime+sI/sampleRate)/60.0/60)
                mean = np.mean(mH)
                meansH.append(mean)
                maxsH.append(np.max(mH))
                minsH.append(np.min(mH))
                stdsH.append(np.std(mH)+mean)
                
        if len(timesL)>0:
            self._LongGraphs['low'].StreamScatter(timesL, meansL , caption=analyte)
            self._LongGraphs['low'].StreamScatter(timesL, maxsL, caption="Max")
            self._LongGraphs['low'].StreamScatter(timesL, minsL, caption="Min")
            self._LongGraphs['low'].StreamScatter(timesL, stdsL, caption="Std")

        if len(meansH)>0:
            self._LongGraphs['high'].StreamScatter(timesH, meansH , caption=analyte)
            self._LongGraphs['high'].StreamScatter(timesH, maxsH, caption="Max")
            self._LongGraphs['high'].StreamScatter(timesH, minsH, caption="Min")
            self._LongGraphs['high'].StreamScatter(timesH, stdsH, caption="Std")    
    
    def RT_Measure(self, analyte, bias_V, measureTime_S, softStart=False, sampleRate=-1, filteredSampleRate=-1, activeChannels=[]):
        self._CreateRTGraphs(  activeChannels=activeChannels)
        print("\n" + analyte, end='')
        outChannels, sampleRate= self._Constant_Read( measureTime_S ,bias_V, sampleRate=sampleRate , plot=True, activeChannels=activeChannels)
        self._CreateSignalGraphs(activeChannels=activeChannels)
        
        self. _LongGraphPoint_RT( analyte,sampleRate, outChannels )
        
        now = datetime.now()
        now =now.strftime('%Y-%m-%d %H_%M_%S')
        if filteredSampleRate!=-1:
            t=np.linspace(0,measureTime_S, len(outChannels['Time']))
            downSample = sampleRate/filteredSampleRate
            

        stepFolder= self.experimentFolder + "\\"  + f"{now}_{analyte}"
        os.mkdir(stepFolder)

         
        serverup=True
        for channel in outChannels:
            if channel!='Time':
                if filteredSampleRate!=-1:
                    down=savgol_filter(outChannels[channel],int(np.min([25, downSample*10])),5)
                    down = np.interp( np.linspace(0,measureTime_S,int(measureTime_S*filteredSampleRate)), t, down)
                    outChannels[channel]=down
                else:
                    down=savgol_filter(outChannels[channel],25,5)    
                    
                self._Graphs[channel].StreamData(down[::10])

                raw_data =  outChannels[channel] 
                max_val = np.max(np.abs(raw_data))
                raw_data = (raw_data/max_val * (2**15-1)).astype(np.int16)

                filename =stepFolder + "\\"  + f"{now} {analyte} C-{channel}_RT.npy" 
                with open(filename,'wb') as f:
                    np.save(f,"RT")
                    np.save(f,now)
                    np.save(f,analyte)
                    np.save(f,channel)
                    np.save(f,sampleRate)
                    np.save(f,bias_V)
                    np.save(f,measureTime_S)
                    np.save(f,max_val)
                    np.save(f,raw_data)

                if serverup:
                    serverup=self._FileNotify( filename)
                    
        return stepFolder,outChannels

    def LIV_Measure(self, analyte, maxVoltage_V, secondsPerPoint, ChangePerPointV=.005 ,  delayTimePercent=.75, sampleRate=-1,  activeChannels=[]):
        cycles=1
        self._CreateRTGraphs(  activeChannels=activeChannels)
        print("\n" + analyte,end='')
        
        outputSampleRate =  1.0/secondsPerPoint
        numPoints = 4*maxVoltage_V/ChangePerPointV
        slew_V_S =ChangePerPointV/secondsPerPoint
        print('Output SampleRate (Samples per Second) =',outputSampleRate)
        print('Output slew (V/S) =',slew_V_S)
            
        times,waveform=self._CreateTrianglWave(maxVoltage_V, slew_V_S, outputSampleRate)
        timePerPoint=np.max(times)/len(waveform)
        measureTime_S=times[-1]

        currents = {}
       
        currents['Bias']=np.zeros( len(waveform))

        #walk through the points and average for each one
        for i in range(len(waveform)):
            outChannels, sampleRate= self._Constant_Read( timePerPoint ,waveform[i], sampleRate=sampleRate , plot=True, activeChannels=activeChannels)
            currents['Bias'][i]= waveform[i]
            for channel in outChannels:
                if (channel not in currents):
                    currents[channel]=np.zeros( len(waveform))
                current=outChannels[channel]
                currents[channel][i]=np.mean(current [ int( len(current)* delayTimePercent ):])
                

        #output the averaged currents
        outChannels = currents
        self._CreateIVGraphs( activeChannels=activeChannels)
        
        now = datetime.now()
        now =now.strftime('%Y-%m-%d %H_%M_%S')


        bias=outChannels['Bias'] 
        raw_bias = bias
        max_bias = np.max(np.abs(raw_bias))
        raw_bias = (raw_bias/max_bias * (2**15-1)).astype(np.int16)
        
        stepFolder= self.experimentFolder + "\\"  + f"{now}_{analyte}"
        os.mkdir(stepFolder)
        serverup=True
        for channel in outChannels:
           
            if channel!='Bias' and channel!='Time':
                raw_data =  outChannels[channel] 
                
                self._Graphs[channel].StreamScatter(bias,raw_data)
                max_val = np.max(np.abs(raw_data))
                raw_data = (raw_data/max_val * (2**15-1)).astype(np.int16)

                filename=stepFolder + "\\"  + f"{now} {analyte} C-{channel}_LV.npy"
                with open(filename,'wb') as f:
                    np.save(f,"LV")
                    np.save(f,now)
                    np.save(f,analyte)
                    np.save(f,channel)
                    np.save(f,sampleRate)
                    np.save(f,outputSampleRate)
                    np.save(f,maxVoltage_V)
                    np.save(f,slew_V_S)
                    np.save(f,cycles)
                    np.save(f,measureTime_S)
                    np.save(f,max_bias)
                    np.save(f,raw_bias)
                    np.save(f,max_val)
                    np.save(f,raw_data)
                    
                if serverup:
                    serverup=self._FileNotify( filename)
        return stepFolder,outChannels            

    def IV_Measure(self, analyte, maxVoltage_V, slew_V_S, cycles=1, sampleRate=-1, outputSampleRate=-1,filteredSampleRate=-1, activeChannels=[]):
        self._CreateIVGraphs(  activeChannels=activeChannels)
        print("\n" + analyte,end='')
        if outputSampleRate ==-1:
            outputSampleRate = 10000
            
        times,waveform=self._CreateTrianglWave(maxVoltage_V, slew_V_S, outputSampleRate)
        measureTime_S=np.max(times)
        
        outChannels,inSampleRate=self._Waveform_Read( waveform , outSampleRate=outputSampleRate, inSampleRate =sampleRate,  plot=True,  activeChannels=activeChannels)
        
        self._CreateIVGraphs( activeChannels=activeChannels)
        
        now = datetime.now()
        now =now.strftime('%Y-%m-%d %H_%M_%S')


        bias=outChannels['Bias'] 
        if filteredSampleRate!=-1:
            t=np.linspace(0,measureTime_S, len(outChannels['Bias']))
            downSample = inSampleRate/filteredSampleRate
            bias = savgol_filter(bias,int(np.min([25, downSample*10])),5)
            bias = np.interp( np.linspace(0,measureTime_S,int(measureTime_S*filteredSampleRate)), t, bias)

        raw_bias = bias
        max_bias = np.max(np.abs(raw_bias))
        raw_bias = (raw_bias/max_bias * (2**15-1)).astype(np.int16)
        
        stepFolder= self.experimentFolder + "\\"  + f"{now}_{analyte}"
        os.mkdir(stepFolder)
        serverup=True
        for channel in outChannels:
            if channel!='Bias':
                if filteredSampleRate!=-1:
                    down=savgol_filter(outChannels[channel],int(np.min([25, downSample*10])),5)
                    down = np.interp( np.linspace(0,measureTime_S,int(measureTime_S*filteredSampleRate)), t, down)
                    outChannels[channel]=down
                else:
                    down=savgol_filter(outChannels[channel],25,5)    
             
                self._Graphs[channel].StreamScatter(bias[::10],down[::10])

                raw_data =  outChannels[channel] 
                max_val = np.max(np.abs(raw_data))
                raw_data = (raw_data/max_val * (2**15-1)).astype(np.int16)

                filename=stepFolder + "\\"  + f"{now} {analyte} C-{channel}_IV.npy"
                with open(filename,'wb') as f:
                    np.save(f,"IV")
                    np.save(f,now)
                    np.save(f,analyte)
                    np.save(f,channel)
                    np.save(f,sampleRate)
                    np.save(f,outputSampleRate)
                    np.save(f,maxVoltage_V)
                    np.save(f,slew_V_S)
                    np.save(f,cycles)
                    np.save(f,measureTime_S)
                    np.save(f,max_bias)
                    np.save(f,raw_bias)
                    np.save(f,max_val)
                    np.save(f,raw_data)
                    
                if serverup:
                    serverup=self._FileNotify( filename)
        return stepFolder,outChannels        

    def QuickIVParams(self, channelData, slew_V_s):
        conductances={}
        capacitance ={}
        error={}
        bias = channelData['Bias']
        for channel in channelData:
            if channel!='Time' and channel !='Bias':
                currents = channelData[channel]
                sI,eI = int( len(currents)*.55), int( len(currents)*.7)
                cur,V = currents[sI:eI],bias[sI:eI]
                fit = np.polyfit(V,cur,1)
                conductances [channel]=fit[0]
                capacitance[channel]= -1*fit[1]/slew_V_s
                error[channel]= np.mean( np.abs( np.poly1d( fit)(V)-cur))
            
        return conductances,capacitance,error
        
    def _saveDLChunk(self, f,   times, outChannelsMean, outChannelsMax,outChannelsMin,outChannelsStd ):
        np.save(f, 'Segment')
        np.save(f, times)
        for channel in outChannelsMean:
            np.save(f,channel)
            mean_data = np.array( outChannelsMean[channel] )
            np.save(f,mean_data)
            max_data =  np.array( outChannelsMax[channel] )
            np.save(f,max_data)
            min_data =  np.array( outChannelsMin[channel] )
            np.save(f,min_data)
            std_data =  np.array( outChannelsStd[channel] )
            np.save(f,std_data)
        
            outChannelsMean[channel]=[]
            outChannelsMax[channel]=[]
            outChannelsMin[channel]=[]
            outChannelsStd[channel]=[]
        np.save(f,"EndSegment")
        times =[]
        return times, outChannelsMean, outChannelsMax,outChannelsMin,outChannelsStd

    def _LongGraphPoint_DL(self, analyte,  outChannelsMean, outChannelsMax,outChannelsMin,outChannelsStd):
        x=[ (time.time()-self.startTime)/60.0/60 ]
        meansL =[]
        maxsL =[]
        minsL =[]
        meansH =[]
        maxsH =[]
        minsH =[]
        for channel in outChannelsMean:
            if channel!="Time" and channel!="Bias":
                means = np.abs(outChannelsMean[channel])
                mean = np.mean(means )
                max = np.max( means)
                if mean>1:
                    meansH.append(mean)
                    maxsH.append( max)
                    minsH.append(np.min( means))
                elif max<13.8:
                    meansL.append(mean)
                    maxsL.append(max)
                    minsL.append(np.min( means))
                
        if len(meansL)>0:self._LongGraphs['low'].StreamScatter(x, [np.mean(meansL)] , caption=analyte)
        if len(maxsL)>0:self._LongGraphs['low'].StreamScatter(x, [np.max(maxsL)], caption="Max")
        if len(minsL)>0:self._LongGraphs['low'].StreamScatter(x, [np.min(minsL)], caption="Min")
        if len(meansL)>0:self._LongGraphs['low'].StreamScatter(x, [np.std(meansL)+np.mean(meansL)], caption="Std")

        if len(meansH)>0:self._LongGraphs['high'].StreamScatter(x, [np.mean(meansH)] , caption=analyte)
        if len(maxsH)>0:self._LongGraphs['high'].StreamScatter(x, [np.max(maxsH)], caption="Max")
        if len(minsH)>0:self._LongGraphs['high'].StreamScatter(x, [np.min(minsH)], caption="Min")
        if len(meansH)>0:self._LongGraphs['high'].StreamScatter(x, [np.std(meansH)+np.mean(meansH)], caption="Std")        
        
    def _MeasureLog(self,analyte, bias_V, measureTime_Hr, sampleRate, f, activeChannels=[],dead_time_s=0,cycleVoltage=True):
        times = []
        outChannelsMax= {}
        outChannelsMin = {}
        outChannelsMean = {}
        outChannelsStd = {}
        startTime = time.time()
        longTime = time.time()
        sampleTime = 1/sampleRate
        longDuration = 60
        i=0
        while (time.time()-startTime)/60/60<measureTime_Hr:
            i+=1
            if dead_time_s!=0:
                if cycleVoltage:
                    self.Zero()
                time.sleep(dead_time_s)
                
            tChannels, sampleRate= self._Constant_Read( sampleTime ,bias_V, sampleRate=-1 , plot=False,  activeChannels=activeChannels)
            times.append(time.time()-startTime)
             
            for channel in tChannels:
                if channel not in outChannelsMax:
                    outChannelsMax[channel]=[]
                    outChannelsMin[channel]=[]
                    outChannelsMean[channel]=[]
                    outChannelsStd[channel]=[]
                outChannelsMean[channel].append( np.mean( tChannels[channel]))
                outChannelsMax[channel].append( np.max( tChannels[channel]))
                outChannelsMin[channel].append( np.min( tChannels[channel]))
                outChannelsStd[channel].append( np.std( tChannels[channel]))
                if i%4==0 and i!=0 and channel!="Time":
                    x=np.array( times[-4:])/60.0/60.0
                    y= outChannelsMean[channel][-4:]
                    self._Graphs[channel].StreamScatter(x,y , caption="Mean")
                    self._Graphs[channel].StreamScatter(x, outChannelsMax[channel][-4:], caption="Max")
                    self._Graphs[channel].StreamScatter(x, outChannelsMin[channel][-4:], caption="Min")
                    self._Graphs[channel].StreamScatter(x, np.array(outChannelsStd[channel][-4:])+np.array(outChannelsMean[channel][-4:]), caption="Std")

            if time.time()- longTime>=longDuration:
                self._LongGraphPoint_DL(analyte, outChannelsMean, outChannelsMax,outChannelsMin,outChannelsStd)
                longTime = time.time()
                 

            if len( times)>600:
                self._saveDLChunk( f, times, outChannelsMean, outChannelsMax,outChannelsMin,outChannelsStd )
         
        if len(times)>0:                  
            self._saveDLChunk( f, times, outChannelsMean, outChannelsMax,outChannelsMin,outChannelsStd )
            
    def _ReformatLogFile(self,stepFolder ):
        outChannelsMax= {}
        outChannelsMin = {}
        outChannelsMean = {}
        outChannelsStd = {}
        times=[]
        
        with open(stepFolder + "\\logFile.npy",'rb') as f:
            now = np.load(f)
            analyte=    np.load(f)
            
            sampleRate =  np.load(f)
            bias_V = np.load(f)
            measureTime_Hr  = np.load(f)

            
            segmentIndicator =    str( np.load(f))
            
            while segmentIndicator=='Segment':
                _times = np.load(f)
                channel =str( np.load(f))
                
                while channel!="EndSegment":
                    if channel not in outChannelsMean:
                        outChannelsMean[channel]=[]
                        outChannelsMax[channel]=[]
                        outChannelsMin[channel]=[]
                        outChannelsStd[channel]=[]
                    
                    mean_data = np.load(f)
                    max_data = np.load(f)
                    min_data = np.load(f)
                    std_data = np.load(f)
    
                    times.append(times)
                    outChannelsMean[channel].append(mean_data)
                    outChannelsMax[channel].append(max_data)
                    outChannelsMin[channel].append(min_data)
                    outChannelsStd[channel].append(std_data)   
                    channel =str( np.load(f))
                     
                segmentIndicator =    str( np.load(f))
                
        serverup=True
        for channel in outChannelsMean:
            
            if channel!='Time':
                filename=stepFolder + "\\"  + f"{now} {analyte} C-{channel}_DL.npy"
                with open(filename,'wb') as f:
                    np.save(f,"DL")
                    np.save(f,now)
                    np.save(f,analyte)
                    np.save(f,channel)
                    np.save(f,sampleRate)
                    np.save(f,bias_V)
                    np.save(f,measureTime_Hr)
                    
                    mean_data = np.concatenate(  outChannelsMean[channel] )
                    max_val = np.max(np.abs(mean_data))
                    mean_data = (mean_data/max_val * (2**15-1)).astype(np.int16)
                    np.save(f,max_val)
                    np.save(f,mean_data)

                    mean_data = np.concatenate(  outChannelsMin[channel] )
                    max_val = np.max(np.abs(mean_data))
                    mean_data = (mean_data/max_val * (2**15-1)).astype(np.int16)
                    np.save(f,max_val)
                    np.save(f,mean_data)

                    mean_data = np.concatenate(  outChannelsMax[channel] )
                    max_val = np.max(np.abs(mean_data))
                    mean_data = (mean_data/max_val * (2**15-1)).astype(np.int16)
                    np.save(f,max_val)
                    np.save(f,mean_data)

                    mean_data = np.concatenate(  outChannelsStd[channel] )
                    max_val = np.max(np.abs(mean_data))
                    mean_data = (mean_data/max_val * (2**15-1)).astype(np.int16)
                    np.save(f,max_val)
                    np.save(f,mean_data)
                if serverup:
                    serverup=self._FileNotify( filename)
        
        
        #os.remove(stepFolder + "\\logFile.npy")
        return times, outChannelsMean, outChannelsMax,outChannelsMin,outChannelsStd

    def CheckData(self, channelData):
        unshorted=[]
        shorted=[]
        means ={}
        maxs = {}
        for channel in channelData:
            if channel!='Time' and channel!='Bias':
                means[channel]=np.mean(channelData[channel])
                maxs[channel]=np.max(channelData[channel])
                if maxs[channel]>14:
                    shorted.append(channel)
                else:
                    unshorted.append(channel)
        return shorted,unshorted,means,maxs
            
    def CheckShorted(self):
        #we do not want to save this data, only using it to determine which channels are shorted
        channelData, sampleRate= self._Constant_Read(1 ,.05)

        #get a list of the shorted devices
        shorted,activeChannels,means,maxs = self.CheckData( channelData)
        print('Active channels',activeChannels)
        return shorted,activeChannels
        
    def DL_Measure(self, analyte, bias_V, measureTime_Hr, samplePeriod_s, dead_time_s=0, cycleVoltage=True, softStart=False, activeChannels=[]):
        self._CreateNultiGraphs( activeChannels=activeChannels,xlabel="Time(hr)")
        print("\n" + analyte, end='')
        now = datetime.now() .strftime('%Y-%m-%d %H_%M_%S')
        stepFolder= self.experimentFolder + "\\"  + f"{now}_{analyte}"
        os.mkdir(stepFolder)

        with open(stepFolder + "\\logFile.npy" ,'wb') as f:
            np.save(f,now)
            np.save(f,analyte)
            np.save(f,(1/samplePeriod_s))
            np.save(f,bias_V)
            np.save(f,measureTime_Hr)
            self._MeasureLog(analyte, bias_V, measureTime_Hr, (1/samplePeriod_s) ,f, activeChannels,dead_time_s=dead_time_s,cycleVoltage=cycleVoltage )
            np.save(f,'Finish')
        
        times, outChannelsMean, outChannelsMax,outChannelsMin,outChannelsStd=self._ReformatLogFile(stepFolder )
        return stepFolder , outChannelsMean, outChannelsMax,outChannelsMin,outChannelsStd

    def _FileNotify(self, filename):
        try:
            myobj = {'filename': filename.lower().replace("e:\\","\\\\biod2100\\DataFolder\\" ),'wafer':self.wafer ,'chip':self.chip }
            x = requests.post( 'https://10.212.27.176:7003/RaxDataNotify', json = myobj, verify=False, timeout=.5)
            return True
        except:
            print('No Server',end=" ")
            return False
        




