
import os
import numpy as np
from datetime import datetime
import matplotlib.pyplot as plt
import pandas as pd
from matplotlib import colormaps
from scipy.signal import savgol_filter
import re
import seaborn as sns

def SaveIVsTo_CVS(analytes,analyteList, filename_csv):
    type='IV'
    epoch_time = datetime(2024, 1, 1)
    if len(analyteList)==0:
        for analyte in analytes:
            if len(analytes[analyte][type])>0:
                analyteList.append(analyte)
                
    folders = analytes[analyteList[0]][type]
    channels ={}
    minTime =0
    channelNames = [] 
    for folder in folders:
        files = [ x for x in  os.listdir(folder)]
        channelNames .extend( [ x.split('C-')[1].split('_')[0] for x in files if 'C-' in x])
    channelNames=list(set(channelNames))

    for analyte in analytes:
        folders = analytes[analyte ][type]
        for channelName in channelNames:
            times=[]
            slopes=[]
            caps=[]
            for folder in folders:
                files = [ f"{folder}/{x}" for x in  os.listdir(folder) if f'C-{channelName}_' in x]
                for file in files:
                    fileInfo = LoadDataFile(file)
                    fileTime = (fileInfo['fileTime'] - epoch_time).total_seconds()
                    if minTime ==0:
                        minTime = fileTime
                    fileTime = fileTime - minTime
                    sR = fileInfo['sampleRate']
                    bias = fileInfo['bias']
                    current = fileInfo['mean_data']
                    times.append(fileTime)
                    p= np.polyfit(bias,current,1)
                    slopes.append(p[0])
                    caps.append( np.abs( p[1]/fileInfo['slew']))
                     
            channels[channelName]={'times':times,'slopes':slopes,'caps':caps}
    
        timeCol = [] 
        simple = {}
        for channel in channels:
            if len(timeCol)==0:
                timeCol = channels[channel]['times']
                conds = channels[channel]['slopes']
                simple['time(s)']=timeCol
                #channels[channel]=''
            else:
                conds = np.interp(timeCol,channels[channel]['times'], channels[channel]['slopes'])
                #channels[channel]=''
            simple[channel]=conds
        df=pd.DataFrame(simple)
        df.to_csv(filename_csv, mode='a', index=False)   
    
        timeCol = [] 
        simple = {}
        for channel in channels:
            if len(timeCol)==0:
                timeCol = channels[channel]['times']
                caps = channels[channel]['caps']
                simple['time(s)']=timeCol
            else:
                caps = np.interp(timeCol,channels[channel]['times'], channels[channel]['caps'])
            simple[channel]=caps
        df=pd.DataFrame(simple)
        df.to_csv(filename_csv.replace('.','_cap.'), mode='a', index=False) 

def SaveRT(analytes,analyteList,filename):
    type='RT'
    epoch_time = datetime(2024, 1, 1)
    if len(analyteList)==0:
        for analyte in analytes:
            if len(analytes[analyte][type])>0:
                analyteList.append(analyte)
                
    folders = analytes[analyteList[0]][type]
    minTime =0
    channelNames = [] 
    for folder in folders:
        files = [ x for x in  os.listdir(folder)]
        channelNames .extend( [ x.split('C-')[1].split('_')[0] for x in files])
    channelNames=list(set(channelNames))
    cc=0
    for analyte in analytes:
        folders = analytes[analyte ][type]
        for folder in folders:
            files = [ f"{folder}/{x}" for x in  os.listdir(folder)  ]
            channels ={}
            for file in files:
                fileInfo = LoadDataFile(file)
                channel=fileInfo['channel']
                analyte = fileInfo['analyte']
                fileTime = (fileInfo['fileTime'] - epoch_time).total_seconds()
                if minTime ==0:
                    minTime = fileTime
                fileTime = fileTime - minTime
                sR = fileInfo['sampleRate']
                
                means = fileInfo['mean_data'][::5000]
                channels[channel]=means    
                if 'time' not in channels:
                    channels['time']=fileTime+np.linspace(0,len(means)/(sR/5000),len(means))
            df=pd.DataFrame(channels)
            if cc==0:
                df.to_csv(filename, index=False)  
            else:
                df.to_csv(filename, index=False,mode='a', header=False)  
             
            cc+=1
    
def LoadExperimentStructure(wafer,chip):
    experimentFolder = f'E:/DataFolder/{wafer}/{chip}'
    dirs = [ f"{experimentFolder}/{x}" for x in  os.listdir(experimentFolder)]
    dirs = sorted(dirs)
    analytes ={}
    for dir in dirs:
        if '.' not in dir:
            foldername = (os.path.basename(dir))
            parts = foldername.split('_')
            analyte = re.sub(r'\d{4}-\d{2}-\d{2}\s\d{2}_\d{2}_\d{2}_', '', foldername)
            files=[x for x in os.listdir(dir) if '.npy' in x and 'logFile' not in x]
            if len(files)==0:
                continue
            type = files[0].split('_')[-1].split('.')[0]
            
            if (analyte not in analytes):
                analytes[analyte]={'RT':[],'IV':[],'DL':[]}
            try:
                analytes[analyte][type].append(dir)
            except:
                print('missing',analyte,type)
    for analyte in analytes:
        types = ''
        if len(analytes[analyte]['RT'])>0:
            types +="RT "
        if len(analytes[analyte]['IV'])>0:
            types +="IV "
        if len(analytes[analyte]['DL'])>0:
            types +="DL "

        print(analyte + ":" + types)
    return analytes      


def PlotIV(analytes,analyte, plotConductance, plotCapacitance, saveFolder=''):
    type='IV'
    epoch_time = datetime(2024, 1, 1)
    
    
    folders = analytes[analyte][type]
    channels ={}
    minTime =0
    channelNames = [] 
    for folder in folders:
        files = [ x for x in  os.listdir(folder)]
        channelNames .extend( [ x.split('C-')[1].split('_')[0] for x in files if 'C-' in x])
    channelNames=list(set(channelNames))

    for channelName in channelNames:
        plt.figure(figsize=(15,3))
        times=[]
        slopes=[]
        caps = []
        
        for folder in folders:
            files = [ f"{folder}/{x}" for x in  os.listdir(folder) if f'C-{channelName}_' in x]
            count=0
            for file in files:
                 
                fileInfo = LoadDataFile(file)
                fileTime = (fileInfo['fileTime'] - epoch_time).total_seconds()
                if minTime ==0:
                    minTime = fileTime
                fileTime = fileTime - minTime
                sR = fileInfo['sampleRate']
                bias = fileInfo['bias']
                current = fileInfo['mean_data']
                
                sbias=np.diff(savgol_filter(bias, 15, 1))
                times.append(fileTime)
                bias=bias[:len(sbias)]
                current=current[:len(sbias)]
                x=np.linspace(0,len(current),len(current))+count
                #plt.plot(x , current)
                count = x[-1]
                bX,bY= bias[ (sbias<-.0001) & (bias<0) ],current[(sbias<-.0001) & (bias<0)]
                p= np.polyfit(bX,bY,1)
                #plt.plot(bias,current)
                #plt.plot(bX,bY)
                #plt.plot(bX, np.poly1d(p)(bX))
                #plt.show()
                slopes.append(p[0])
                caps.append( np.abs( p[1]/fileInfo['slew']))
              
        times = np.array(times)
        channels[channelName]={'times':times,'slopes':slopes}

        if plotConductance and plotCapacitance:
            fig, ax1 = plt.subplots()
            color = 'tab:red'
            ax1.plot( times/60/60, slopes , color=color)
            ax1.set_ylabel('Conductance(nS)', color=color)
            ax1.set_xlabel('time (hrs)')
            ax1.tick_params(axis='y', labelcolor=color)
            
            ax2=ax1.twinx() 
            color = 'tab:blue'
            ax2.plot( times/60/60, caps , color=color)
            ax2.set_ylabel('Capacitance(nF)', color=color)
            ax2.tick_params(axis='y', labelcolor=color)
            
        elif plotConductance:
            plt.plot( times/60/60, slopes  )
            plt.ylabel('Conductance(nS)')
        elif plotCapacitance:
            plt.plot( times/60/60, caps  )
            plt.ylabel('Capacitance(nF)')
        plt.title(channelName)
        plt.xlabel('Time(hrs)')
        if (saveFolder!=''):
            plt.savefig( saveFolder + "/" + channelName + ".png", format="png")
        plt.show()
         
          
    return channels

def SaveDL(analytes,analyteList,filename):
    type='DL'
    epoch_time = datetime(2024, 1, 1)
    if len(analyteList)==0:
        for analyte in analytes:
            if len(analytes[analyte][type])>0:
                analyteList.append(analyte)
                
    folders = analytes[analyteList[0]][type]
    minTime =0
    channelNames = [] 
    for folder in folders:
        files = [ x for x in  os.listdir(folder)]
        channelNames .extend( [ x.split('C-')[1].split('_')[0] for x in files if 'C-' in x])
    channelNames=list(set(channelNames))

    channels ={}
    cc=0
    for analyte in analytes:
        folders = analytes[analyte ][type]
        for folder in folders:
            files = [ f"{folder}/{x}" for x in  os.listdir(folder)  ]
            for file in files:
                try:
                    fileInfo = LoadDataFile(file)
                    channel=fileInfo['channel']
                    analyte = fileInfo['analyte']
                    fileTime = (fileInfo['fileTime'] - epoch_time).total_seconds()
                    if minTime ==0:
                        minTime = fileTime
                    fileTime = fileTime - minTime
                    sR = fileInfo['sampleRate']
                    means = fileInfo['mean_data'][::100]
                    channels[channel]=means    
                except:
                    pass
            df=pd.DataFrame(channels)
            if cc==0:
                df.to_csv(filename, index=False)  
            else:
                df.to_csv(filename, index=False,mode='a', header=False)  
             
            cc+=1



def PlotAllIV(analytes,analyteList=[],   plotCapacitance=False,  saveDefault=False, saveFolder=''):
    type='IV'
    epoch_time = datetime(2024, 1, 1)
    
    if len(analyteList)==0:
        for analyte in analytes:
            if len(analytes[analyte][type])>0:
                analyteList.append(analyte)
    
    folders = analytes[analyteList[0]][type]
    
    minTime =0
    channelNames = [] 
    for folder in folders:
        files = [ x for x in  os.listdir(folder)]
        channelNames .extend( [ x.split('C-')[1].split('_')[0] for x in files if 'C-' in x])
    baseFolder = "/".join(folder.replace("\\","/").split('/')[:4])
    if saveDefault:
        saveFolder = baseFolder + "/GraphsIV"
        if plotCapacitance:
            saveFolder += "_Cap"
        print("Saving graphs to:" , saveFolder)
        if os.path.isdir(saveFolder)==False:
            os.mkdir(saveFolder)
    channelNames=list(set(channelNames))

    for channelName in channelNames:
        plt.figure(figsize=(15,3))
        
        for analyte in analyteList:
            times=[]
            slopes=[]
            caps = []
            folders = analytes[analyte][type]
            for folder in folders:
                files = [ f"{folder}/{x}" for x in  os.listdir(folder) if f'C-{channelName}_' in x]
                count=0
                for file in files:
                    fileInfo = LoadDataFile(file)
                    fileTime = (fileInfo['fileTime'] - epoch_time).total_seconds()
                    if minTime ==0:
                        minTime = fileTime
                    fileTime = fileTime - minTime
                    sR = fileInfo['sampleRate']
                    bias = fileInfo['bias']
                    current = fileInfo['mean_data']
                    
                    sbias=np.diff(savgol_filter(bias, 15, 1))
                    
                    bias=bias[:len(sbias)]
                    current=current[:len(sbias)]
                    x=np.linspace(0,len(current),len(current))+count
                    count = x[-1]
                    bX,bY= bias[ (sbias<-.0001) & (bias<0) ],current[(sbias<-.0001) & (bias<0)]
                    if len(bX)>3:
                        times.append(fileTime)
                        p= np.polyfit(bX,bY,1)
                        slopes.append(p[0])
                        caps.append( np.abs( p[1]/fileInfo['slew']))
                  
            times = np.array(times)
            if plotCapacitance==False:
                plt.plot( times/60/60, slopes  , label=analyte)
                plt.ylabel('Conductance(nS)')
            else:
                plt.plot( times/60/60, caps , label=analyte)
                plt.ylabel('Capacitance(nF)')
        plt.title(channelName)
        plt.legend()
        plt.xlabel('Time(hrs)')
        if (saveFolder!=''):
            plt.savefig( saveFolder + "/" + channelName + ".png", format="png")
        plt.show()


def PlotEnsembleIV(analytes,analyteList=[],   plotCapacitance=False,     saveDefault=False, ylimCon=[-1000,-1000], ylimCap=[-1000,-1000], saveFolder=''):
    type='IV'
    epoch_time = datetime(2024, 1, 1)
    if len(analyteList)==0:
        for analyte in analytes:
            if len(analytes[analyte][type])>0:
                analyteList.append(analyte)
    
    folders = analytes[analyteList[0]][type]
    
    minTime =0
    channelNames = [] 
    for folder in folders:
        files = [ x for x in  os.listdir(folder)]
        channelNames .extend( [ x.split('C-')[1].split('_')[0] for x in files if 'C-' in x])
    baseFolder = "/".join(folder.replace("\\","/").split('/')[:4])
    if saveDefault:
        saveFolder = baseFolder + "/GraphsIV"
        if plotCapacitance:
            saveFolder += "_Cap"
        print("Saving graphs to:" , saveFolder)
        if os.path.isdir(saveFolder)==False:
            os.mkdir(saveFolder)
    channelNames=list(set(channelNames))
    channelStart = {}


    firstIV = {}
    lastIV={}
    
    fig,ax=plt.subplots(4,1,figsize=(15,12),sharex=True)
    for channelName in channelNames:
        for analyte in analyteList:
            times=[]
            slopes=[]
            caps = []
            folders = analytes[analyte][type]
            for folder in folders:
                files = [ f"{folder}/{x}" for x in  os.listdir(folder) if f'C-{channelName}_' in x]
                count=0
                for file in files:
                    fileInfo = LoadDataFile(file)
                    fileTime = (fileInfo['fileTime'] - epoch_time).total_seconds()
                    if minTime ==0:
                        minTime = fileTime
                    fileTime = fileTime - minTime
                    sR = fileInfo['sampleRate']
                    bias = fileInfo['bias']
                    current = fileInfo['mean_data']
                    
                    sbias=np.diff(savgol_filter(bias, 15, 1))
                    
                    bias=bias[:len(sbias)]
                    current=current[:len(sbias)]
                    x=np.linspace(0,len(current),len(current))+count
                    if channelName not in firstIV:
                        firstIV[channelName]=[bias,current]
                    lastIV [channelName]=[bias,current]
                    count = x[-1]
                    bX,bY= bias[ (sbias<-.0001) & (bias<0) ],current[(sbias<-.0001) & (bias<0)]
                    if len(bX)>3:
                        times.append(fileTime)
                        p= np.polyfit(bX,bY,1)
                        slopes.append(p[0])
                        caps.append( np.abs( p[1]/fileInfo['slew']))
                  
            times = np.array(times)
            if channelName not in channelStart:
                 channelStart [channelName]= [slopes[0],caps[0]]
             
            ax[0].plot( times/60/60, slopes -channelStart[channelName][0]  , label=analyte)
            ax[1].plot( times/60/60, caps-channelStart[channelName][1]  , label=analyte)
            ax[2].semilogy( times/60/60, slopes   , label=analyte)
            ax[3].semilogy( times/60/60, caps   , label=analyte)
           
    ax[0].set_title('IV Conductance')
    ax[1].set_title('IV Capacitance')
    ax[2].set_title('IV Conductance')
    ax[3].set_title('IV Capacitance')
    
    ax[0].set_ylabel('Delta Conductance(nS)')
    ax[1].set_ylabel('Delta Capacitance(nF)')

    ax[2].set_ylabel('Conductance(nF)')
    ax[3].set_ylabel('Capacitance(nF)')
    ax[3].set_xlabel('Time(hrs)')
    if (ylimCon[0]!=-1000):
        ax[0].set_ylim(ylimCon)
    if (ylimCap[0]!=-1000):
        ax[1].set_ylim(ylimCap)
    if (saveFolder!=''):
        plt.savefig( saveFolder + "/EnsembleIV.png", format="png")
    plt.show()

    for channelName in firstIV:
        plt.plot(firstIV[channelName][0],firstIV[channelName][1],label='first')
        plt.plot(lastIV[channelName][0],lastIV[channelName][1],label='first')
        plt.title(channelName)
        plt.xlabel('Volts (v)')
        plt.ylabel('Current (nA)')
        plt.legend()
        plt.show()


def PlotEnsembleRT(analytes,analyteList=[],hourUnits=True, saveDefault=False,  showMax=True,showLog=False, ylim=[-1000,-1000], saveFolder=''):
    type='RT'
    epoch_time = datetime(2024, 1, 1)

    if len(analyteList)==0:
        for analyte in analytes:
            if len(analytes[analyte][type])>0:
                analyteList.append(analyte)
    
    folders = analytes[analyteList[0]][type]
    channels ={}
    minTime =0
    channelNames = [] 
    for folder in folders:
        files = [ x for x in  os.listdir(folder)]
        channelNames .extend( [ x.split('C-')[1].split('_')[0] for x in files if 'C-' in x])
    baseFolder = "/".join(folder.replace("\\","/").split('/')[:4])
    if saveDefault:
        saveFolder = baseFolder + "/GraphsRT"
        print("Saving graphs to:" , saveFolder)
        if os.path.isdir(saveFolder)==False:
            os.mkdir(saveFolder)
    
    channelNames=list(set(channelNames))

    divideTime =1
    if hourUnits:
        divideTime = 60*60
    channelStart = {}
    plt.figure(figsize=(15,3))
    folderPile=[]
    print(len(folders))
    for folder in folders:
        dataPile = [] 
        for channelName in channelNames:
            time0=0
            channels[channelName]=[]
            cn=0
        
            for analyte in analyteList:
                folders = analytes[analyte][type]
                fa=0
            
                files = [ f"{folder}/{x}" for x in  os.listdir(folder) if f'C-{channelName}_' in x]
                for file in files:
                    fileInfo = LoadDataFile(file)
                    if channelName not in channelStart:
                        channelStart [channelName]=np.mean( fileInfo['mean_data'][:10])
                    
                    channel=fileInfo['channel']
                    analyte = fileInfo['analyte']
                    fileTime = (fileInfo['fileTime'] - epoch_time).total_seconds()
                    if minTime ==0:
                        minTime = fileTime
                    fileTime = fileTime - minTime
                    sR = fileInfo['sampleRate']
                    means = fileInfo['mean_data']-channelStart [channelName]
                    dataPile.append(means[::100])
            cn+=1
        folderPile.append(dataPile)
    
    for i in range(len(folderPile)):
        folderPile[i]=np.concatenate(folderPile[i])
    sns.violinplot(data=folderPile,   inner="quart", fill=False)
    plt.xlabel('Time')
    plt.ylabel('Current (nA)')
    if (ylim[0]!=-1000):
        plt.ylim(ylim)
    plt.show()
    return folderPile
    
def PlotEnsembleDL(analytes,analyteList=[],hourUnits=True, showMax=True,showLog=False, saveDefault=False, maxY=-1000, minY = -1000, saveFolder=''):
    type='DL'
    epoch_time = datetime(2024, 1, 1)
    if len(analyteList)==0:
        for analyte in analytes:
            if len(analytes[analyte][type])>0:
                analyteList.append(analyte)
    
    folders = analytes[analyteList[0]][type]
     
    channels ={}
    minTime =0
    channelNames = [] 
    for folder in folders:
        files = [ x for x in  os.listdir(folder)]
        channelNames .extend( [ x.split('C-')[1].split('_')[0] for x in files if 'C-' in x])
    baseFolder = "/".join(folder.replace("\\","/").split('/')[:4])
    if saveDefault:
        saveFolder = baseFolder + "/GraphsDL"
        print("Saving graphs to:" , saveFolder)
        if os.path.isdir(saveFolder)==False:
            os.mkdir(saveFolder)
    channelNames=list(set(channelNames))
   
    divideTime =1
    if hourUnits:
        divideTime = 60*60
    plt.figure(figsize=(15,3))        
    channelStart = {}
    for channelName in channelNames:
        time0=0
        channels[channelName]=[]
        cn=0
        for analyte in analyteList:
            folders = analytes[analyte][type]
            fa=0
            for folder in folders:
                files = [ f"{folder}/{x}" for x in  os.listdir(folder) if f'C-{channelName}_' in x]
                
                for file in files:
                    fileInfo = LoadDataFile(file)
                    
                    if channelName not in channelStart:
                        channelStart [channelName]=np.mean( fileInfo['mean_data'][:10])
                   
                    fileTime = (fileInfo['fileTime'] - epoch_time).total_seconds()
                    if minTime ==0:
                        minTime = fileTime
                    fileTime = fileTime - minTime
                    sR = fileInfo['sampleRate']
                    if len(fileInfo['mean_data'])>200:
                        means = fileInfo['mean_data'][::50]
                        mins = fileInfo['min_data'][::50]
                        maxs = fileInfo['max_data'][::50]
                    else:
                        means = fileInfo['mean_data']
                        mins = fileInfo['min_data']
                        maxs = fileInfo['max_data']
                        
                    x=np.linspace(time0,time0+len(means)/sR,len(means))

                    if (showLog):
                        plt.semilogy( x[1:]/divideTime, np.abs(means[1:]-channelStart[channelName]), 'C'+str(cn)  )
                    else:
                        plt.plot( x[1:]/divideTime, means[1:]-channelStart[channelName], 'C'+str(cn)  )
                    if (showMax):
                        if (showLog):
                            plt.plot(x/divideTime,np.abs(maxs-channelStart[channelName]), 'C'+str(cn+2))
                        else:
                            #plt.plot(x/divideTime,mins-channelStart[channelName], 'C'+str(cn+1))
                            plt.plot(x/divideTime,maxs-channelStart[channelName], 'C'+str(cn+2))
                    
                    time0=x[-1]
                    fa=fa+1
            cn+=1
     
     
    if hourUnits:
        plt.xlabel('Measurement Time(hrs)')
    else:
        plt.xlabel('Measurement Time(s)')
    plt.ylabel('Current Change(nA)')
    if (maxY!=-1000):
        plt.ylim([minY,maxY])
    if (saveFolder!=''):
        plt.savefig( saveFolder + "/ensemble_DL.png", format="png")
    plt.show()
          
    
def PlotAllDL(analytes,analyteList=[],hourUnits=True, saveDefault=False, saveFolder=''):
    type='DL'
    epoch_time = datetime(2024, 1, 1)
    if len(analyteList)==0:
        for analyte in analytes:
            if len(analytes[analyte][type])>0:
                analyteList.append(analyte)
    
    folders = analytes[analyteList[0]][type]
     
    channels ={}
    minTime =0
    channelNames = [] 
    for folder in folders:
        files = [ x for x in  os.listdir(folder)]
        channelNames .extend( [ x.split('C-')[1].split('_')[0] for x in files if 'C-' in x])
    baseFolder = "/".join(folder.replace("\\","/").split('/')[:4])
    if saveDefault:
        saveFolder = baseFolder + "/GraphsDL"
        print("Saving graphs to:" , saveFolder)
        if os.path.isdir(saveFolder)==False:
            os.mkdir(saveFolder)
    channelNames=list(set(channelNames))
   
    divideTime =1
    if hourUnits:
        divideTime = 60*60
    for channelName in channelNames:
        plt.figure(figsize=(15,3))
        time0=0
        channels[channelName]=[]
        cn=0
        for analyte in analyteList:
            folders = analytes[analyte][type]
            fa=0
            for folder in folders:
                files = [ f"{folder}/{x}" for x in  os.listdir(folder) if f'C-{channelName}_' in x]
                
                for file in files:
                    fileInfo = LoadDataFile(file)
                    
                    channel=fileInfo['channel']
                   
                    fileTime = (fileInfo['fileTime'] - epoch_time).total_seconds()
                    if minTime ==0:
                        minTime = fileTime
                    fileTime = fileTime - minTime
                    sR = fileInfo['sampleRate']
                    if len(fileInfo['mean_data'])>200:
                        means = fileInfo['mean_data'][::50]
                        mins = fileInfo['min_data'][::50]
                        maxs = fileInfo['max_data'][::50]
                    else:
                        means = fileInfo['mean_data']
                        mins = fileInfo['min_data']
                        maxs = fileInfo['max_data']
                        
                    x=np.linspace(time0,time0+len(means)/sR,len(means))
                    if fa==0:
                        plt.plot( x[1:]/divideTime, means[1:], 'C'+str(cn) ,label=analyte )
                        plt.plot(x/divideTime,mins, 'C'+str(cn+1),label='min')
                        plt.plot(x/divideTime,maxs, 'C'+str(cn+2),label='max')
                    else:
                        plt.plot( x[1:]/divideTime, means[1:], 'C'+str(cn)  )
                        plt.plot(x/divideTime,mins, 'C'+str(cn+1))
                        plt.plot(x/divideTime,maxs, 'C'+str(cn+2))
                    
                    time0=x[-1]
                    fa=fa+1
            cn+=1
        plt.title(channel)
        plt.legend()
        if hourUnits:
            plt.xlabel('Measurement Time(hrs)')
        else:
            plt.xlabel('Measurement Time(s)')
        plt.ylabel('Current(nA)')
        if (saveFolder!=''):
            plt.savefig( saveFolder + "/" + channelName + ".png", format="png")
        plt.show()
        

def PlotAllRT(analytes,analyteList=[],hourUnits=True, saveDefault=False, saveFolder=''):
    type='RT'
    epoch_time = datetime(2024, 1, 1)

    if len(analyteList)==0:
        for analyte in analytes:
            if len(analytes[analyte][type])>0:
                analyteList.append(analyte)
    
    folders = analytes[analyteList[0]][type]
    channels ={}
    minTime =0
    channelNames = [] 
    for folder in folders:
        files = [ x for x in  os.listdir(folder)]
        channelNames .extend( [ x.split('C-')[1].split('_')[0] for x in files if 'C-' in x])
    baseFolder = "/".join(folder.replace("\\","/").split('/')[:4])
    if saveDefault:
        saveFolder = baseFolder + "/GraphsRT"
        print("Saving graphs to:" , saveFolder)
        if os.path.isdir(saveFolder)==False:
            os.mkdir(saveFolder)
    
    channelNames=list(set(channelNames))

    divideTime =1
    if hourUnits:
        divideTime = 60*60
    for channelName in channelNames:
        plt.figure(figsize=(15,3))
        time0=0
        channels[channelName]=[]
        cn=0
        for analyte in analyteList:
            folders = analytes[analyte][type]
            fa=0
            for folder in folders:
                files = [ f"{folder}/{x}" for x in  os.listdir(folder) if f'C-{channelName}_' in x]
                for file in files:
                    fileInfo = LoadDataFile(file)
                    
                    channel=fileInfo['channel']
                    analyte = fileInfo['analyte']
                    fileTime = (fileInfo['fileTime'] - epoch_time).total_seconds()
                    if minTime ==0:
                        minTime = fileTime
                    fileTime = fileTime - minTime
                    sR = fileInfo['sampleRate']
                    means = fileInfo['mean_data'][::100]
                    x=np.linspace(time0,time0+len(fileInfo['mean_data'])/sR,len(means))
                    if fa==0:
                        plt.plot( x[1:]/divideTime, means[1:], 'C'+str(cn) ,label=analyte )
                    else:
                        plt.plot( x[1:]/divideTime, means[1:], 'C'+str(cn)  )
                   
                    
                    time0=x[-1]
                    fa=fa+1
            cn+=1
        plt.title(channel)
        plt.legend()
        if hourUnits:
            plt.xlabel('Measurement Time(hrs)')
        else:
            plt.xlabel('Measurement Time(s)')
        plt.ylabel('Current(nA)')
        if (saveFolder!=''):
            plt.savefig( saveFolder + "/" + channelName + ".png", format="png")
        plt.show()
        
def PlotDL(analytes,analyte,saveFolder=''):
    type='DL'
    epoch_time = datetime(2024, 1, 1)
    folders = analytes[analyte][type]
    channels ={}
    minTime =0
    channelNames = [] 
    for folder in folders:
        files = [ x for x in  os.listdir(folder)]
        channelNames .extend( [ x.split('C-')[1].split('_')[0] for x in files if 'C-' in x])
    channelNames=list(set(channelNames))

    for channelName in channelNames:
        plt.figure(figsize=(15,3))
        time0=0
        channels[channelName]=[]
        for folder in folders:
            files = [ f"{folder}/{x}" for x in  os.listdir(folder) if f'C-{channelName}_' in x]
            for file in files:
                fileInfo = LoadDataFile(file)
                
                channel=fileInfo['channel']
                analyte = fileInfo['analyte']
                fileTime = (fileInfo['fileTime'] - epoch_time).total_seconds()
                if minTime ==0:
                    minTime = fileTime
                fileTime = fileTime - minTime
                sR = fileInfo['sampleRate']
                means = fileInfo['mean_data'][::100]
                x=np.linspace(time0,time0+sR*len(fileInfo['mean_data']),len(means))
                plt.plot( x, means  )
                plt.plot(x,fileInfo['min_data'][::100])
                plt.plot(x,fileInfo['max_data'][::100])
                
                time0=x[-1]
        plt.title(channel)
        plt.xlabel('Time(s)')
        plt.ylabel('Current(nA)')
        if (saveFolder!=''):
            plt.savefig( saveFolder + "/" + channelName + ".png", format="png")
        plt.show()


        
def PlotRT(analytes,analyte,saveFolder):
    type='RT'
    epoch_time = datetime(2024, 1, 1)
    folders = analytes[analyte][type]
    channels ={}
    minTime =0
    channelNames = [] 
    for folder in folders:
        files = [ x for x in  os.listdir(folder)]
        channelNames .extend( [ x.split('C-')[1].split('_')[0] for x in files])
    channelNames=list(set(channelNames))

    for channelName in channelNames:
        plt.figure(figsize=(15,3))
        time0=0
        channels[channelName]=[]
        for folder in folders:
            files = [ f"{folder}/{x}" for x in  os.listdir(folder) if f'C-{channelName}_' in x]
            for file in files:
                fileInfo = LoadDataFile(file)
                channel=fileInfo['channel']
                analyte = fileInfo['analyte']
                fileTime = (fileInfo['fileTime'] - epoch_time).total_seconds()
                if minTime ==0:
                    minTime = fileTime
                fileTime = fileTime - minTime
                sR = fileInfo['sampleRate']
                means = fileInfo['mean_data'][::100]
                x=np.linspace(time0,time0+len(fileInfo['mean_data'])/sR,len(means))
                plt.plot( x, means  )
                time0=x[-1]
        plt.title(channel)
        plt.xlabel('Time(s)')
        plt.ylabel('Current(nA)')
        if (saveFolder!=''):
            plt.savefig( saveFolder + "/" + channelName + ".png", format="png")
        plt.show()


def LoadDataFile(filename):
    fileInfo ={}
    with open(filename,'rb') as f:
        fileType =str( np.load(f))
        fileInfo['fileType']=fileType
        fileInfo['fileTime'] =datetime.strptime( str(np.load(f)), "%Y-%m-%d %H_%M_%S")
        fileInfo['analyte'] =str( np.load(f))
        fileInfo['channel'] = str(np.load(f))
        fileInfo['sampleRate'] = float( np.load(f))
        if fileType=='DL':
            fileInfo['bias'] = float(np.load(f))
            fileInfo['measureTime'] =float( np.load(f))*60*60
            max_val = np.load(f)
            data = np.ravel( np.load(f))
            fileInfo['mean_data'] = max_val*data.astype(float)/(2**15)
            max_val = np.load(f)
            data = np.load(f)
            fileInfo['min_data'] = max_val*data.astype(float)/(2**15)
            max_val = np.load(f)
            data = np.load(f)
            fileInfo['max_data'] = max_val*data.astype(float)/(2**15)
            max_val = np.load(f)
            data = np.load(f)
            fileInfo['std_data'] = max_val*data.astype(float)/(2**15)
        elif fileType=='IV' or fileType=='LV':
            fileInfo['outputSampleRate'] =float( np.load(f))
            fileInfo['bias'] = np.load(f)
            fileInfo['slew'] =float( np.load(f))
            fileInfo['cycles'] = float( np.load(f))
            fileInfo['measureTime'] =float(np.load(f))
            max_val = np.load(f)
            bias = np.load(f)
            fileInfo['bias'] = max_val*bias.astype(float)/(2**15)
            max_val = np.load(f)
            data = np.load(f)
            fileInfo['mean_data'] = max_val*data.astype(float)/(2**15)
        elif fileType=='RT':
            fileInfo['bias'] =float( np.load(f))
            fileInfo['measureTime'] =float( np.load(f))
            max_val = np.load(f)
            data = np.load(f)
            fileInfo['mean_data'] = max_val*data.astype(float)/(2**15)
            
      
    fileInfo['shortAnalyte'] = fileInfo['analyte'].split(fileInfo['fileType'])[0].strip() 
    return fileInfo
    
def DownSampleMinutes(currents, measureTime, pointTime = 60):
    chunks=measureTime/pointTime
    skips =int( len(currents)/chunks)
    
    dt=measureTime/len(currents)
    means = []
    std =[] 
    maxs =[]
    mins=[]
    times =[]
    if skips>0:
        for i in range(0,len(currents)-skips,skips):
            chunk=currents[i:i+skips]
            times.append(dt*i)
            means.append(np.mean(chunk))
            std.append(np.std(chunk))
            maxs.append(np.max(chunk))
            mins.append(np.min(chunk))
    return np.array(times),np.array(means),np.array(std),np.array(maxs),np.array(mins)

def DownSampleDLMinutes(fileInfo ):
    measureTime = fileInfo['measureTime']
    if measureTime>3*60:
        chunkTime = 60
    else:
        chunkTime = measureTime/3
    times,means,_,_,_ = DownSampleMinutes(fileInfo['mean_data'], measureTime, chunkTime)
    _,std,_,_,_= DownSampleMinutes(fileInfo['std_data'], measureTime, chunkTime)
    _,mins ,_,_,_     = DownSampleMinutes(fileInfo['min_data'], measureTime, chunkTime)
    _,maxs ,_,_,_     = DownSampleMinutes(fileInfo['max_data'], measureTime, chunkTime)

    return times,means,std,maxs,mins    