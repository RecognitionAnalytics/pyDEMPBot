import numpy as np
import matplotlib.pyplot as plt
import os
from datetime import datetime
import FileLoaderLib  as fll
import re
#import plotly.io as pio
import plotly.express as px
import plotly.graph_objects as go
import datetime
from scipy.signal import savgol_filter,decimate
import seaborn as sns
 
 
pallets =[ plt.cm.Purples, plt.cm.Blues, plt.cm.Greens, plt.cm.Oranges, plt.cm.Reds,
                      plt.cm.YlOrBr, plt.cm.YlOrRd, plt.cm.OrRd, plt.cm.PuRd, plt.cm.RdPu, plt.cm.BuPu,
                      plt.cm.GnBu, plt.cm.PuBu, plt.cm.YlGnBu, plt.cm.PuBuGn, plt.cm.BuGn, plt.cm.Greys,plt.cm.YlGn]


cmaplist = [] 
for pallet in pallets :
    cmap = []
    for i in range( pallet.N-50,50,-5):
        r,g,b,a = pallet(i)
        cmap.append((r,g,b))
    cmaplist.append(cmap) 
    

def LoadExperimentStructure(wafer,chip):
 
    if os.path.exists(f'//biod1343/DataFolder/{wafer}/{chip}'):
        experimentFolder = f'//biod1343/DataFolder/{wafer}/{chip}'
    elif os.path.exists(f'//biod2100/DataFolder/{wafer}/{chip}'):
        experimentFolder = f'//biod2100/DataFolder/{wafer}/{chip}'
    print('Loading from :' ,experimentFolder)
        
    dirs = [ f"{experimentFolder}/{x}" for x in  os.listdir(experimentFolder)]
    dirs = sorted(dirs)
    analytes = []
    for dir in dirs:
        if '.' not in dir:
            try:
                foldername = (os.path.basename(dir))
                parts = foldername.split('_')
                date = re.search(r'\d{4}-\d{2}-\d{2}\s\d{2}_\d{2}_\d{2}',  foldername) [0]
                
                analyte = re.sub(r'\d{4}-\d{2}-\d{2}\s\d{2}_\d{2}_\d{2}_', '', foldername)
                date=datetime.datetime.strptime(date,'%Y-%m-%d %H_%M_%S')
                files=[x for x in os.listdir(dir) if '.npy' in x and 'logFile' not in x]
                if len(files)==0:
                    continue
                type = files[0].split('_')[-1].split('.')[0]
                
                analyte = analyte.split(type)[0].strip()
                analytes .append( { 'date':date, 'name':analyte,'type':type, 'folder':dir, 'files':files} )
            except:
                pass
    return analytes

def SelectData(analytes, analyteSearch='all', type='all' ):
    analyteNames=[x.strip().lower() for x in analyteSearch.split(',')]
    types=[x.strip().lower() for x in type.split(',')]

    possibleAnalytes = []
    for analyte in analytes:
        legalAnalyte=False
        if analyteSearch =='all':
            legalAnalyte=True
        else:
            for aN in analyteNames:
                if aN in  analyte['name'].lower():
                    legalAnalyte=True
                    break
        if not legalAnalyte:
            continue
            
        legalAnalyte=False
        if type=='all':
            legalAnalyte=True
        else :
            for aN in types:
                if aN == analyte['type'].lower().strip():
                    legalAnalyte=True
                    break
        if not legalAnalyte:
            continue

        possibleAnalytes.append(analyte)
        
    print('Selected Analytes:')
    names=list(set([x['name'] for x in possibleAnalytes]))
    for name in names:
        print(name)
    return possibleAnalytes    


def QuickIVStats(fileInfo):
    current = fileInfo['mean_data']
    bias = fileInfo['bias']
    maxBias = np.max(bias)
    maxConductance = np.max(current)/maxBias

    sbias=np.diff(savgol_filter(bias, 15, 1))
    
    
    bias=bias[:len(sbias)]
    current=current[:len(sbias)]
    x=np.linspace(0,len(current),len(current)) 
 
    
    bX,bY= bias[ (sbias<0) & (np.abs(current)<14) ],current[(sbias<0) & (np.abs(current)<14)]
    if len(bX)<5:
        conductance = 10000
        capacitance =0
    else:
         
        bX,bY=bX[int(len(bX)*.1):int(len(bX)*.9)],bY[int(len(bX)*.1):int(len(bX)*.9)]
        p= np.polyfit(bX,bY,1)
        conductance= p[0]
        capacitance= np.abs( p[1]/fileInfo['slew'])
    fileInfo['mean_data']=np.mean(fileInfo['mean_data'])
    fileInfo['max_bias']=maxBias
    fileInfo['bias']=np.mean(fileInfo['bias'])
    fileInfo['maxConductance']=maxConductance
    fileInfo['conductance']=conductance
    fileInfo['capacitance']=capacitance
    return fileInfo

def PlotEnsembleIVs(allTraces, analyteColors, xlim =[None,None]):
    traces3 = [x for x in allTraces if x['fileType']=='IV']
        
    if len(traces3)==0:
        print('No IV traces found')
        return
        
    #sort traces by time
    traces3 = sorted(traces3, key=lambda x: x['fileTime'])

    channelNames = list(set([x['channel'] for x in traces3]))

    #check for shorted channels
    badChannels = []
    for channelName in channelNames:
        channelTraces = [x for x in traces3 if x['channel']==channelName]
        if channelTraces[0]['conductance']>=10000:
            badChannels.append(channelName)
            
    #remove shorted channels
    channelNames = [x for x in channelNames if x not in badChannels]

    slews = np.unique([int(x['slew']*1000) for x in traces3])
    maxBiasi = np.unique([ int(np.round( x['max_bias']*100)) for x in traces3])

    for  maxBias in maxBiasi:
        for selectSlew in slews:
            traces = [x for x in traces3 if int(x['slew']*1000)==selectSlew and int(np.round( x['max_bias']*100))==maxBias]
             
            if len(traces)==0:
                continue
            _,ax=plt.subplots(1,2,figsize=(10,5))
            cc=0
            for channelName in channelNames:
                transitions = []
                aa=0
                for analyte in analyteColors:
                    
                    analyteMap = analyteColors[analyte]
                    
                    dataPoints = [x for x in traces if x['channel']==channelName and x['shortAnalyte']==analyte]
                     
                    if len(dataPoints)==0:
                        continue
                    dataPoints = sorted( dataPoints, key=lambda x: x['fileTime'])
                    conductance = [ x['conductance']  for x in dataPoints]
                    capacitance = [ x['capacitance']  for x in dataPoints]
                    time_Hrs = [ x['time_Hrs']     for x in dataPoints]
                    
                    color =analyteMap[cc%len(analyteMap)]
                    transitions.append( {'analyte':analyte, 'lastConductance':conductance[-1], 'lastCapacitance':capacitance[-1], 'lastTime':time_Hrs[-1], 
                                        'firstConductance':conductance[0], 'firstCapacitance':capacitance[0], 'firstTime':time_Hrs[0], 'color':color})
                    
                    
                    if (aa==0 and cc==0):
                        ax[0].scatter(time_Hrs[0],conductance[0],   color=color, label=analyte.strip('_'))
                        ax[1].scatter(time_Hrs[0],capacitance[0],   color=color, label=analyte.strip('_'))
                        
                    ax[0].semilogy(time_Hrs,conductance,   color=color)
                    ax[1].semilogy(time_Hrs,capacitance,   color=color)
                    aa+=1
                            
                if len(conductance)==1:
                    ax[0].semilogy([time_Hrs[0],time_Hrs[0]+1],[conductance[0],conductance[0]],  color=color)
                    ax[1].semilogy([time_Hrs[0],time_Hrs[0]+1],[capacitance[0],capacitance[0]],  color=color)
                    ax[0].scatter(time_Hrs[0]+1,conductance[0],  color='k')
                    ax[1].scatter(time_Hrs[0]+1,capacitance[0],  color='k')
                
                #sort transitions by time
                transitions = sorted(transitions, key=lambda x: x['firstTime'])
                for i in range(1,len(transitions)):
                    if cc==0:
                        ax[0].scatter( transitions[i]['firstTime'],transitions[i]['firstConductance'], label=transitions[i]['analyte'].strip('_'), color=transitions[i]['color'])
                        ax[1].scatter( transitions[i]['firstTime'],transitions[i]['firstCapacitance'], label=transitions[i]['analyte'].strip('_'), color=transitions[i]['color'])
                    else:
                        ax[0].scatter( transitions[i]['firstTime'],transitions[i]['firstConductance'],  color=transitions[i]['color'])
                        ax[1].scatter( transitions[i]['firstTime'],transitions[i]['firstCapacitance'],   color=transitions[i]['color'])
                    #plot a line between each transition
                    ax[0].plot( [transitions[i-1]['lastTime'],transitions[i]['firstTime']],
                                [transitions[i-1]['lastConductance'],transitions[i]['firstConductance']],
                                color=transitions[i-1]['color'])
                    ax[1].plot( [transitions[i-1]['lastTime'],transitions[i]['firstTime']],
                                [transitions[i-1]['lastCapacitance'],transitions[i]['firstCapacitance']],
                                color=transitions[i-1]['color'])
                cc+=1
                
            plt.suptitle(f'Max Voltage = { maxBias /100} @ Slew = {selectSlew/1000} V/s')
            ax[0].set_xlabel('Time (hr)')
            ax[0].set_ylabel('Conductance (nS)')
            ax[1].set_xlabel('Time (hr)')
            ax[1].set_ylabel('Capacitance (nF)')
            #place the legend outside the second plot on the right side 
            ax[1].legend(loc='center left', bbox_to_anchor=(1, 0.5))
            if (xlim[0] is not None) and (xlim[1] is not None):
                ax[0].set_xlim(xlim)
                ax[1].set_xlim(xlim)
            plt.show()      
    
    
def PlotEnsembleDLs(allTraces,  analyteColors,dataType='mean', xlim =[None,None]): 
    traces = [x for x in allTraces if x['fileType']=='DL']

    if len(traces)==0:
        print('No DL traces found')
        return
        
    #sort traces by time
    traces = sorted(traces, key=lambda x: x['fileTime'])
    channelNames = list(set([x['channel'] for x in traces]))
    #check for shorted channels
    badChannels = []
    for channelName in channelNames:
        channelTraces = [x for x in traces if x['channel']==channelName]
        if np.mean( channelTraces[0]['mean_data'])>=14:
            badChannels.append(channelName)
            
    #remove shorted channels
    channelNames = [x for x in channelNames if x not in badChannels]

    biases = np.unique([int(x['bias']*1000) for x in traces])

    _,ax=plt.subplots(len(biases),1,figsize=(10,5*len(biases)), sharex=True)
    if len(biases)==1:
        ax=[ax]
    cc=0
    analytesUsed = []
    for channelName in channelNames:
        dataPoints = [x for x in traces if x['channel']==channelName    ]
        
        lastTime =np.zeros(len(biases))
        for trace in dataPoints:
            analyte = trace['shortAnalyte']
            bias = trace['bias']
            
            if bias<0 and dataType=='max':
                tDataType='min_data'
            elif bias>0 and dataType=='min':
                tDataType='max_data'
            else:
                tDataType=dataType+ '_data'
            current =  trace[tDataType ]  
            


            graphIndx = np.argwhere(biases==int(bias*1000))[0][0]
            #check if we have already plotted this analyte
            if analyte + str(graphIndx)  in analytesUsed:
                firstUse = False
            else:
                firstUse = True
                analytesUsed.append(analyte+ str(graphIndx))
            
            time_Hrs = lastTime[graphIndx] + np.linspace(0,len(current) * trace['sampleRate'],len(current))/60/60
            
            analyteMap = analyteColors[analyte]
            color =analyteMap[cc%len(analyteMap)]
            
            if firstUse:
                ax[graphIndx].semilogy(time_Hrs, np.abs( current), color=color,label = analyte.strip().strip('_'))
            else:
                ax[graphIndx].semilogy(time_Hrs, np.abs( current), color=color)
            
            lastTime[graphIndx] = time_Hrs[-1]
            
        cc+=1

        
        
    for i in range(len(biases)):
        ax[i].set_ylabel('Abs Current (nA)')
        ax[i].set_xlabel('Time (hr)')
        ax[i].set_title(  f'{dataType} @ Bias = {biases[i]/1000} V')
        ax[i].legend(loc='center left', bbox_to_anchor=(1, 0.5))
        
        if xlim[0] is not None:
            ax[i].set_xlim(xlim)

    plt.show()   
    

def PlotEnsembleRTViolins(allTraces, analyteColors, showLog =True):

    traces = [x for x in allTraces if x['fileType']=='RT']
        
    if len(traces)==0:
        print('No RT traces found')
        return
        
    #sort traces by time
    traces = sorted(traces, key=lambda x: x['fileTime'])

    channelNames = list(set([x['channel'] for x in traces]))

    #check for shorted channels
    badChannels = []
    for channelName in channelNames:
        channelTraces = [x for x in traces if x['channel']==channelName]
        if np.mean( channelTraces[0]['mean_data'])>=14:
            badChannels.append(channelName)
            
    #remove shorted channels
    channelNames = [x for x in channelNames if x not in badChannels]
    
    
    biases = np.unique([int(x['bias']*1000) for x in traces])

    
    
    for selectbias in biases:
        cc=0
        folderPile = {}
        startCurrent ={}
        xlabels =[]
        
        aColor = {}
        for analyte in analyteColors:
            dataPile = []
            
            
            dataPoints = [x for x in traces if x['shortAnalyte']==analyte and int(x['bias']*1000)==selectbias]
            dataPoints = sorted( dataPoints, key=lambda x: x['fileTime'])
            nPoints = np.sum ( [len(x['mean_data']) for x in dataPoints if x['channel'] in channelNames[0]])
            for dataPoint in dataPoints:
                channelName = dataPoint['channel']
                if channelName not in channelNames:
                    continue
                current = dataPoint['mean_data']
                if channelName not in startCurrent:
                    startCurrent[channelName]=np.mean(  current[-1000:] )
                    
                if nPoints>100000:
                    current = decimate( current-startCurrent[channelName],10)
                else:
                    current = current-startCurrent[channelName]
                
                if (showLog):
                    current = np.log( np.abs( current)+1e-9)
                dataPile.extend(current)
                
            if len(dataPile)>0:
                xlabels.append(analyte.strip('_').strip())
                folderPile[analyte.strip('_').strip()]= (dataPile)
                aColor[analyte.strip('_').strip()]=analyteColors[ analyte][-1]
            
                
        plt.figure(figsize=(7,4))
        ax=sns.violinplot(data=folderPile,   density_norm='width', inner='point', palette=aColor, cut=0, linewidth=1)
        #ax.set_xticklabels(xlabels)
        plt.ylabel('Log(Delta Current (nA))')
        plt.title(f'RT @ Bias = {selectbias/1000} V')
        plt.show() 
        
            
def PlotEnsembleRTs(allTraces,analyteColors):

    traces = [x for x in allTraces if x['fileType']=='RT']

    if len(traces)==0:
        print('No DL traces found')
        return
        
    #sort traces by time
    traces = sorted(traces, key=lambda x: x['fileTime'])
    channelNames = list(set([x['channel'] for x in traces]))
    #check for shorted channels
    badChannels = []
    for channelName in channelNames:
        channelTraces = [x for x in traces if x['channel']==channelName]
        if np.mean( channelTraces[0]['mean_data'])>=14:
            badChannels.append(channelName)
            
    #remove shorted channels
    channelNames = [x for x in channelNames if x not in badChannels]

    biases = np.unique([int(x['bias']*1000) for x in traces])

    _,ax=plt.subplots(len(biases),1,figsize=(10,5*len(biases)))
    cc=0
    analytesUsed = []
    for channelName in channelNames:
        dataPoints = [x for x in traces if x['channel']==channelName    ]
        
        lastTime =np.zeros(len(biases))
        for trace in dataPoints:
            analyte = trace['shortAnalyte']
            bias = trace['bias']
            current =  trace['mean_data' ]  

            graphIndx = np.argwhere(biases==int(bias*1000))[0][0]
            #check if we have already plotted this analyte
            if analyte + str(graphIndx)  in analytesUsed:
                firstUse = False
            else:
                firstUse = True
                analytesUsed.append(analyte+ str(graphIndx))
            
            time_Hrs = lastTime[graphIndx] + np.linspace(0,len(current) * trace['sampleRate'],len(current))/60/60
            
            analyteMap = analyteColors[analyte]
            color =analyteMap[cc%len(analyteMap)]
            
            if firstUse:
                ax[graphIndx].semilogy(time_Hrs, np.abs( current), color=color,label = analyte.strip().strip('_'))
            else:
                ax[graphIndx].semilogy(time_Hrs, np.abs( current), color=color)
            
            lastTime[graphIndx] = time_Hrs[-1]
        
            
        cc+=1

        
        
    for i in range(len(biases)):
        ax[i].set_ylabel('Abs Current (nA)')
        ax[i].set_xlabel('Time (hr)')
        ax[i].set_title(  f'@ Bias = {biases[i]/1000} V')
        ax[i].legend(loc='center left', bbox_to_anchor=(1, 0.5))
        
        #if xlim[0] is not None:
        #    ax[i].set_xlim(xlim)

    plt.show()               
    
    
def PlotEnsembleIVsPL(allTraces, analyteColors, reverseLogTime = False):
    traces = [x for x in allTraces if x['fileType']=='IV']
        
    if len(traces)==0:
        print('No IV traces found')
        return
        
    #sort traces by time
    traces = sorted(traces, key=lambda x: x['fileTime'])

    channelNames = list(set([x['channel'] for x in traces]))

    #check for shorted channels
    badChannels = []
    for channelName in channelNames:
        channelTraces = [x for x in traces if x['channel']==channelName]
        if channelTraces[0]['conductance']>=10000:
            badChannels.append(channelName)
            
    #remove shorted channels
    channelNames = [x for x in channelNames if x not in badChannels]
    channelNames = sorted(channelNames)
 
    tranceAnalytes= np.unique([x['shortAnalyte'] for x in traces])
    print(tranceAnalytes)
    fig = go.Figure()
    cc=0
    for channelName in channelNames:
        transitions = []
        
        times = []
        conductances = []
        analyteTimes = []
        analyteNames = []
        for analyte in tranceAnalytes:
            analyteMap = analyteColors[analyte]
            dataPoints = [x for x in traces if x['channel']==channelName and x['shortAnalyte']==analyte]
            
            dataPoints = sorted( dataPoints, key=lambda x: x['fileTime'])
            conductance = [ x['conductance']  for x in dataPoints]
            
            time_Hrs = [ x['time_Hrs']     for x in dataPoints]
            
            combinedTime = []
            combinedConductance = []
            i=0
            while i<len(time_Hrs):
                time = time_Hrs[i]
                meanC = conductance[i]
                meanT = time_Hrs[i]
                ccMean=1
                jLast = i+1 
                for j in range(i+1,len(time_Hrs)):
                    if np.abs( time_Hrs[j]-time_Hrs[i])*60<15:
                        meanC+=conductance[j]
                        meanT+=time_Hrs[j]
                        ccMean+=1
                        jLast = j+1
                
                combinedTime.append(meanT/ccMean)
                combinedConductance.append(meanC/ccMean)
                i=jLast
            
            times.extend(combinedTime)
            conductances.extend(combinedConductance)
            
            if cc==0:
                analyteTimes.append(time_Hrs[0])
                analyteNames.append(analyte.strip('_'))
                
        indx= np.argsort(times)
        times = np.array(times)[indx]
        maxTime = times[-1]
        if reverseLogTime:
            times = np.log(maxTime -times +.1)*-1
            analyteTimes = np.log( maxTime- np.array(analyteTimes)+.1)*-1
            
        conductances = np.array(conductances)[indx]
        fig.add_trace(go.Scatter(x=times, y=conductances, mode='lines+markers', name=channelName.strip('_')))
        for time,name in zip(analyteTimes,analyteNames):
            fig.add_trace(go.Scatter(x=[time,time], y=[0,1000], text=name.strip('_'), mode='lines',line=dict(color="#000000", width=5)))
            fig.add_annotation(x=time, y=0, text= name.strip('_'), showarrow=True, yshift=-100)
            
        
        cc+=1                

    fig.update_yaxes(type="log")
    xlabel = 'Time (hrs)'
    
    if reverseLogTime:
        xlabel = '-1*Log Time (hrs)'
        
    fig.update_layout(
        title=f"Conductance", 
        xaxis_title=xlabel,
        yaxis_title="Conductance (nS)",
        legend_title="Channels",
        font=dict(
            family="Courier New, monospace",
            size=15,
            color="Black"
        )
    )

    fig.show()    
    
def PlotEnsembleIVsSort(allTraces, analyteColors, reverseLogTime = False):

    traces3 = [x for x in allTraces if x['fileType']=='IV']
        
    if len(traces3)==0:
        print('No IV traces found')
        #return
        
    #sort traces by time
    traces3 = sorted(traces3, key=lambda x: x['fileTime'])

    channelNames = list(set([x['channel'] for x in traces3]))

    #check for shorted channels
    badChannels = []
    for channelName in channelNames:
        channelTraces = [x for x in traces3 if x['channel']==channelName]
        if channelTraces[0]['conductance']>=10000:
            badChannels.append(channelName)
            
    #remove shorted channels
    channelNames = [x for x in channelNames if x not in badChannels]
    channelNames = sorted(channelNames)

    slews = np.unique([int(x['slew']*1000) for x in traces3])
    maxBiasi = np.unique([ int(np.round(x['max_bias']*100)) for x in traces3])
    
    for  maxBias in maxBiasi:
        for selectSlew in slews:
            traces = [x for x in traces3 if int(x['slew']*1000)==selectSlew and int(np.round( x['max_bias']*100))==maxBias]
            
            if len(traces)==0:
                continue
            tranceAnalytes= np.unique([x['shortAnalyte'] for x in traces])
            print(tranceAnalytes)
            fig = go.Figure()
            cc=0
            for channelName in channelNames:
                
            
                times = []
                conductances = []
                analyteTimes = []
                analyteNames = []
                for analyte in tranceAnalytes:
                    
                    dataPoints = [x for x in traces if x['channel']==channelName and x['shortAnalyte']==analyte]
                
                    dataPoints = sorted( dataPoints, key=lambda x: x['fileTime'])
                    conductance = [ x['conductance']  for x in dataPoints]
                    
                    time_Hrs = [ x['time_Hrs']     for x in dataPoints]
                    
                    
                    times.extend(time_Hrs)
                    conductances.extend(conductance)
                    
                    if cc==0:
                        analyteTimes.append(time_Hrs[0])
                        analyteNames.append(analyte.strip('_'))
                        
                indx= np.argsort(times)
                times = np.array(times)[indx]
                
                if reverseLogTime:
                    maxTime = times[-1]
                    times = np.log(maxTime -times +.1)*-1
                    analyteTimes = np.log( maxTime- np.array(analyteTimes)+.1)*-1
                
                conductances = np.array(conductances)[indx]
                fig.add_trace(go.Scatter(x=times, y=conductances, mode='lines+markers', name=channelName.strip('_')))
                for time,name in zip(analyteTimes,analyteNames):
                    fig.add_trace(go.Scatter(x=[time,time], y=[0,1000], text='', mode='lines',line=dict(color="#000000", width=5)))
                    fig.add_annotation(x=time, y=0, text= name.strip('_'), showarrow=True, yshift=-100)
                    
                
                cc+=1                
            
            fig.update_yaxes(type="log")
            fig.update_layout(
                title=f"Conductance @{maxBias/100} V, {selectSlew/1000} V/s", 
                xaxis_title="Time (hrs)",
                yaxis_title="Conductance (nS)",
                legend_title="Channels",
                font=dict(
                    family="Courier New, monospace",
                    size=18,
                    color="Black"
                )
            )
            
            fig.show()        
            
def PlotEnsembleDLsPL(allTraces,  dataType, reverseLogTime = False, limitChannels = []):
    traces = [x for x in allTraces if x['fileType']=='DL']

    if len(traces)==0:
        print('No DL traces found')
        #return
        
    #sort traces by time
    traces = sorted(traces, key=lambda x: x['fileTime'])
    channelNames = list(set([x['channel'] for x in traces]))
    #check for shorted channels
    badChannels = []
    for channelName in channelNames:
        channelTraces = [x for x in traces if x['channel']==channelName]
        if np.mean( channelTraces[0]['mean_data'])>=14:
            badChannels.append(channelName)
            
    #remove shorted channels
    channelNames = [x for x in channelNames if x not in badChannels]
    if (len(limitChannels)>0):
        channelNames = [x for x in channelNames if x in limitChannels]

    biases = np.unique([int(x['bias']*1000) for x in traces])
    for selectedBias in biases:
        cc=0
        fig = go.Figure()
        analytesUsed = []
        for channelName in channelNames:
            dataPoints = [x for x in traces if x['channel']==channelName  and int(x['bias']*1000)==selectedBias] 
            
            lastTime = 0
            times =[]
            datas =[]
            analyteNames = []
            
            for trace in dataPoints:
                analyte = trace['shortAnalyte']
                bias = trace['bias']
                
                if bias<0 and dataType=='max':
                    tDataType='min_data'
                elif bias>0 and dataType=='min':
                    tDataType='max_data'
                else:
                    tDataType=dataType+ '_data'
                current =  trace[tDataType ]  

                #check if we have already plotted this analyte
                if analyte  not  in analytesUsed:
                    analytesUsed.append(analyte )
                
                time_Hrs = lastTime + np.linspace(0,len(current) * trace['sampleRate'],len(current))/60/60

                
                times.extend(time_Hrs)
                datas.extend(np.abs( current))
                analyteNames.extend([analyte for x in time_Hrs])
                
                lastTime = time_Hrs[-1]
                
            indx = np.argsort(times)
            times = np.array(times)[indx]
            datas = np.array(datas)[indx]
            analyteNames = np.array(analyteNames)[indx]
            
            analyteTimes=[]
            for analyte in analytesUsed:
                analyteTimes.append(times[np.argwhere(analyteNames==analyte)[0][0]])
                
            if reverseLogTime:
                maxTime = times[-1]
                times = np.log(maxTime -times +.1)*-1
                analyteTimes = np.log( maxTime- np.array(analyteTimes)+.1)*-1
                
            if cc==0:
                for firstTime,analyte in zip(analyteTimes,analytesUsed):
                    fig.add_trace(go.Scatter(x=[firstTime,firstTime], y=[0,1000], name=analyte.strip('_'), mode='lines',line=dict(color="#000000", width=5)))
                    fig.add_annotation(x=firstTime, y=0, text= analyte.strip('_'), showarrow=True, yshift=-100)

            
            if len(times)>100000:
                times = decimate(times,10)
                datas = decimate(datas,10)
                
            fig.add_trace(go.Scatter(x=times, y=datas, mode='lines', name=channelName.strip('_')))
            cc+=1

            
        fig.update_yaxes(type="log")
        
        if reverseLogTime:
            xlabel = '-1*Log Time (hrs)'
        else:
            xlabel = 'Time (hrs)'
        fig.update_layout(
            title=f"DL @ {selectedBias/1000} V", 
            xaxis_title=xlabel,
            yaxis_title=f"Abs Current ({dataType}) (nA)",
            legend_title="Channels",
            font=dict(
                family="Courier New, monospace",
                size=15,
                color="Black"
            )
        )
        fig.show()        
        
        
        

def LoadDatas(selectedAnalytes,voltages = []):
    traces=[]
    firstTime =0
    for analyte in selectedAnalytes:
        channels ={}
        minTime =0
        channelNames = [] 
        channelNames .extend( [ x.split('C-')[1].split('_')[0] for x in analyte['files'] if 'C-' in x])
        channelNames=sorted(list(set(channelNames)))
        for channelName in channelNames:
            
            files = [ f"{analyte['folder']}/{x}" for x in analyte['files'] if f'C-{channelName}_' in x]
            count=0
            for file in files:
                fileInfo = fll.LoadDataFile(file)
                if firstTime ==0:
                    firstTime=fileInfo['fileTime']
                    
                fileInfo['fileName']=file
                keepFile=False
                if len(voltages )==0:
                    keepFile=True
                else:
                    fileBias = np.max(fileInfo['bias'])
                    for volt in voltages:
                        if np.abs(volt-fileBias)<.01:
                            keepFile=True
                            break
                if keepFile:
                    if fileInfo['fileType']=='IV':
                        fileInfo=QuickIVStats(fileInfo)
                    if fileInfo['fileType']=='RT':
                        fileInfo['mean_data']=decimate( decimate(fileInfo['mean_data'],10),10)
                    fileInfo['time_Hrs']= (fileInfo['fileTime']-firstTime).total_seconds()/60/60
                    traces.append(fileInfo)
                    

    #create a dictionary of colors for each analyte in traces

    analyteColors = {}
    for i,trace in enumerate(traces):
        if trace['shortAnalyte'] not in analyteColors:
            analyteColors[trace['shortAnalyte']]=cmaplist[i%len(cmaplist)]           
    
    return traces,analyteColors