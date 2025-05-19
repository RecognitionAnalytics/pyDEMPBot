import numpy as np
import plotly.io as pio
#pio.renderers.default = "vscode"
import seaborn as sns
import pandas as pd
import os
import re
import glob
from scipy.signal import savgol_filter,decimate
import datetime
import matplotlib      # pip install matplotlib
matplotlib.use('agg')
import matplotlib.pyplot as plt
import FileLoaderLib  as fll
import base64
from io import BytesIO
from dash import Dash, dcc, html, Input, Output,  set_props
import plotly.express as px
import plotly.graph_objects as go

sns.set(style="whitegrid")


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
        
    
    return possibleAnalytes    

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
              
                date = re.search(r'\d{4}-\d{2}-\d{2}\s\d{2}_\d{2}_\d{2}',  foldername) [0]
                
                analyte = re.sub(r'\d{4}-\d{2}-\d{2}\s\d{2}_\d{2}_\d{2}_', '', foldername)
                
                date=datetime.datetime.strptime(date,'%Y-%m-%d %H_%M_%S')
                files=[x for x in os.listdir(dir) if '.npy' in x and 'logFile' not in x]
                if len(files)==0:
                    continue
                type = files[0].split('_')[-1].split('.')[0]
                 
                analyte = analyte.split(type)[0].strip()
                
                channelNames = [ x.split('C-')[1].split('_')[0] for x in files if 'C-' in x]
                channelNames=list(set(channelNames))
            
                
            
                analytes .append( { 'date':date, 'name':analyte,'type':type, 'folder':dir, 'files':files, 'channelNames':channelNames} )
            except Exception as e:
                print('Error with:',dir,str(e))
                pass
    return analytes

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
         
        conductance=  p[0] if p[0]>0 else np.abs(p[0]/2)
        capacitance= np.abs( p[1]/fileInfo['slew'])
        
    fileInfo['c-']=bY
    fileInfo['b-']=bX
    fileInfo['mean_data']=np.mean(fileInfo['mean_data'])
    fileInfo['max_bias']=maxBias
    fileInfo['bias']=np.mean(fileInfo['bias'])
    fileInfo['maxConductance']=maxConductance
    fileInfo['conductance']=conductance
    fileInfo['capacitance']=capacitance
    return fileInfo
    
class Dashboard:
    def __init__(self):
        self.wafer =''
        self.chips=[]
        self.chip = ''
        self.dataTypes = ['None']
        self.dataType = ''
        self.analytes = []
        self.analyteNames = []
        self.selectedAnalytes = []
        self.channelNames = []
        self.selectedChannels = []
        self.allTraces =[]
        self.traceFingerPrints = ''
        self.graphMode = ''
        self.logPlot= True
        self.timeMode = 'Normal'
        
        self.scanselection = []
        self.scanselectionIndex = -1
        self.ScanSelection = []
        self.ScanSelectionIndex = -1
        self.voltButtons =[]
        self.slewButtons =[]
        self.graphWidth=1400
        self.IVMode ='conductance'
        self.backColor ="White"
        self.colors=["White","LightSteelBlue"]
        
        self.pandasData = pd.DataFrame()
           
    def CreateLayout(self):
        wafers =[ x.split('\\')[-1] for x in glob.glob(r'\\biod1343\DataFolder\*')]
        wafers.extend([ x.split('\\')[-1] for x in glob.glob(r'\\biod2100\DataFolder\*')])
        wafers= sorted( list(set(wafers)))[::-1]
        # chips = self.loadChips(wafers[0])
       
        
        return html.Div([
            #float button on right
            html.Button('Reload', id='reload', n_clicks=0,  style={"color": "white"}),
            html.Div([
                html.Div([
                    html.P("Wafer", style={"color": "white"}),
                    dcc.Dropdown(
                        id='wafer', 
                        options=wafers,
                        value=wafers[0]
                    ),                ]),
                html.Div([ 
                     
                    html.P("Chip", style={"color": "white"}),
                    dcc.Dropdown(
                        id='chip', 
                        options=[],
                        value=''
                    )]),
            ], style={'columnCount': 2}),
            html.P(''),
            html.Div([
                html.Div([ ],id='dataTypes'),
                html.P('-',id='spacer',  style={"color": "white"}), 
            ], style={'columnCount': 2}),
            html.P("Analytes", style={"color": "white"}),
            dcc.Checklist(
                id='analytes', 
                options= self.analyteNames,
                value=[ ],
                style={"color": "white"}
                ),  
            html.P("Channel", style={"color": "white"}),
            html.Div([
                dcc.Checklist(
                    id='channel', 
                    options=self.channelNames,
                    value=[],
                    inline=True,
                    style={"color": "white"}
                ),
                html.Button('All', id='allChannels', n_clicks=0,  style={"color": "white", "display": "block", "margin-left": "auto", "margin-right": "0"}),
                html.Button('Shorted',id='shorted', n_clicks=0,  style={"color": "white", "display": "block", "margin-left": "auto", "margin-right": "0"}),
            ]),
            html.Div([
                html.P ("Y Scale", style={"color": "white"}),
                dcc.Dropdown(
                    id='logDD',
                    options = ['log', 'linear'],
                    value='log'
                    ),
                html.P ("Time Scale", style={"color": "white"}),
                dcc.Dropdown(
                    id='timeDD',
                    options = ['Normal', 'Reverse Log', 'Step', 'Cut'],
                    value='Normal'
                    ),
             ], style={'columnCount': 2}),
           
            html.Div( [] ,id='selectParams'),
            html.Div( [] ,id='selectScans'),
            html.Div( [] ,id='selectOther'),
            html.P('Graph Width', style={"color": "white"}),
            dcc.Slider(id='widthSlider', min=10, max=100, step=5, value=100, marks={x: str(x) for x in [20, 40, 60, 80]}),
            html.Div( [] ,id='selectGraph'),
            html.Div( [] ,id='selectExport'),
            html.P('-',id='graphSpacer',  style={"color": "white"}), 
            html.Div( [] ,id="graphs"),
            
        ],  style={"background-color": "black","height":"1800px"})    
    
    def loadChips(self,wafer):
        chips = [ x.split('\\')[-1] for x in glob.glob(f'//biod1343/DataFolder/{wafer}/*')]
        chips.extend([ x.split('\\')[-1] for x in glob.glob(f'//biod2100/DataFolder/{wafer}/*')])
        chips = sorted( list(set(chips)))[::-1]
        return chips
    
    def loadAnalytes(self,wafer,chip):
        self.analytes=    LoadExperimentStructure(wafer,chip)
        self.analyteNames = []
        for a in self.analytes:
            if a['name'] not in self.analyteNames:
                self.analyteNames.append(a['name'])
        

    def appChipWafer(self,app):
        
        @app.callback(
            Output('wafer','options'),
            Input('reload', 'n_clicks'),
            Input('wafer', 'options'))
        def reload(n_clicks,wafers):
            if (n_clicks>0):
                self.wafer = ''
                self.chips = []
                wafers =[ x.split('\\')[-1] for x in glob.glob(r'\\biod1343\DataFolder\*')]
                wafers.extend([ x.split('\\')[-1] for x in glob.glob(r'\\biod2100\DataFolder\*')])
                wafers= sorted( list(set(wafers)))[::-1]
            return wafers
            
        @app.callback(
            Output('chip','options'),
            Output('chip','value'),
            Input('wafer', 'value'))
        def change_wafer(wafer ):
            self.chips = self.loadChips(wafer)
            self.wafer = wafer
            self.chip = ''
            self.selectedChannels = []
            self.selectedAnalytes = []
            self.graphMode = ''
            return self.chips, self.chip
        
        @app.callback(
            Output('dataTypes','children'),
            Input('chip', 'value'))
        def change_chip(chip):
            self.chip = chip
            if self.chip!='':
                self.loadAnalytes(self.wafer,chip)
                self.dataTypes = [x['type'] for x in self.analytes]
                self.dataTypes = list(set(self.dataTypes))
            else:
                self.dataTypes = ['None']
            
            if len(self.dataTypes) == 0:
                self.dataTypes = ['None']
                
            dataTypes = ['IV','RT','DL','LV']
                
            buttons = []
            for dataType in dataTypes:
                #if dataType not in dataTypes make color greyed
                if dataType in self.dataTypes:
                    style = {"color": "white"}
                    nClicks = 0
                else:
                    style = {"color": "grey"}
                    nClicks=-1000
                buttons .append( html.Button('Load ' + dataType, id='DataType' + dataType, n_clicks=nClicks,  style=style) )
              
            self.selectedChannels = []
            self.selectedAnalytes = []
            self.graphMode = ''
            
            return buttons

    def appDataType(self,app):
        @app.callback(
            output= dict(
                analyteOptions= Output('analytes','options',allow_duplicate=True),
                analyteValues = Output('analytes','value'),
                channelOptions= Output('channel','options',allow_duplicate=True),
                voltageOptions= Output('selectParams', 'children',allow_duplicate=True),
                sweepOptions=   Output('selectScans', 'children',allow_duplicate=True),
                selectGraph=    Output('selectGraph','children'),
                RTButtonO=      Output('DataTypeRT', 'n_clicks'),   
                IVButton=       Output('DataTypeIV', 'n_clicks'),
                DLButton=       Output('DataTypeDL', 'n_clicks'),
                otherButtons=   Output('selectOther','children'),
            ),
            inputs= dict(
                RTButton=Input('DataTypeRT', 'n_clicks'),
                IVButton=Input('DataTypeIV', 'n_clicks'),
                DLButton=Input('DataTypeDL', 'n_clicks'),
                LVButton=Input('DataTypeLV', 'n_clicks'),
            ),
            prevent_initial_call=True,
            running=[(Output("spacer", "children"), 'Loading', ' - ')])
        def change_dataType(RTButton,IVButton,DLButton, LVButton):
            
            dataType=''
            if RTButton>0:
                dataType = 'RT'
            elif IVButton>0 :
                dataType = 'IV'
            elif DLButton>0:
                dataType = 'DL'
            elif  LVButton>0:
                dataType = 'LV'
                
            output = dict(
                analyteOptions= [],
                analyteValues=  [],
                channelOptions= [],
                voltageOptions= [],
                sweepOptions=   [],
                selectGraph=    [],
                otherButtons=   [],
                RTButtonO=0,  
                IVButton=0,  
                DLButton=0,  
            )
             
            if dataType== self.dataType:
                output['analyteOptions'] = self.analyteNames
                output['channelOptions'] = self.channelNames
                output['voltageOptions'] = self.voltButtons
                output['sweepOptions'] = self.slewButtons
                
            self.dataType = dataType
            
            if dataType=='':
                return output

            self.voltButtons,self.slewButtons=self.LoadDataType()
            
            if self.dataType != '':
                self.analyteNames = [x['name'] for x in self.analytes if x['type']==self.dataType]
                self.analyteNames = list(set(self.analyteNames))
                self.analyteNames = sorted(self.analyteNames)
            else:
                self.analyteNames = []
            
            graphButtons = []
            if self.dataType == 'IV' or self.dataType =='LV':
                graphButtons = [ html.Button('Individual Graphs', id='bSingles', n_clicks=0,  style={"color": "white"}),
                        html.Button('Ensemble', id='bEnsemble', n_clicks=0,  style={"color": "white"}),
                        html.Button('Export Data', id='bExport', n_clicks=0,  style={"color": "white"}),
                        ]
                output['otherButtons']=[ dcc.RadioItems(
                        id='bCapacitance', 
                        options=['Conductance','Capacitance'],
                        value='Conductance',
                        inline=True, 
                        style={"color": "white"}
                    )]
                
                
            elif self.dataType == 'DL':
                graphButtons = [ html.Button('Individual Graphs', id='bSingles', n_clicks=0,  style={"color": "white"})   , 
                        html.Button('Ensemble', id='bEnsemble', n_clicks=0,  style={"color": "white"})  ,
                        html.Button('Export Data', id='bExport', n_clicks=0,  style={"color": "white"}),]
            elif self.dataType == 'RT':
                graphButtons = [ html.Button('Individual Graphs', id='bSingles', n_clicks=0,  style={"color": "white"}),
                        html.Button('Ensemble', id='bEnsemble', n_clicks=0,  style={"color": "white"}),
                        html.Button('Violin', id='bViolin', n_clicks=0,  style={"color": "white"})  ,
                        html.Button('Export Data', id='bExport', n_clicks=0,  style={"color": "white"}),]
                
            self.ScanSelectionIndex = -1
            self.VoltSelectionIndex = -1                

            output['analyteValues']= self.analyteNames
            output['analyteOptions']= self.analyteNames
            output['voltageOptions']= self.voltButtons
            output['sweepOptions']= self.slewButtons
            output['selectGraph']= graphButtons
            return output
            
        @app.callback(
            Output('channel','options'),
            Output('channel','value',allow_duplicate=True),
            Input('analytes', 'value'), 
            prevent_initial_call=True,)
        def change_analytes(analytes):
            self.selectedAnalytes = analytes
            self.channelNames = []
            for a in self.analytes:
                if a['type']==self.dataType and a['name'] in self.selectedAnalytes:
                    self.channelNames.extend(a['channelNames'])
            self.channelNames = sorted( list(set(self.channelNames)))
            if (self.selectedChannels == []):
                self.selectedChannels = self.channelNames
            return self.channelNames,self.selectedChannels
        
        @app.callback(
            Output('channel','value',allow_duplicate=True),
            Output('allChannels','children'),
            Input('allChannels', 'n_clicks'), 
            prevent_initial_call=True,)
        def allChannels(n_clicks):
            title='None'
            if n_clicks>0:
                if n_clicks%2==0:
                    self.selectedChannels = self.channelNames
                    title='None'
                else:
                    self.selectedChannels = []
                    title='All'
            return self.selectedChannels,title
        
        @app.callback(
            Output('selectExport','children',allow_duplicate=True),
            Input('bExport', 'n_clicks'),
            prevent_initial_call=True,)
        def changeExport(n_clicks):
            if n_clicks>0:
                return [dcc.Input(id='exportFilename', value='e:\\temp\\' + self.wafer + self.chip + self.dataType + ".csv"),
                        html.Button('Save', id='bSave', n_clicks=0,  style={"color": "white"})]
            
        @app.callback(
            Input('bSave', 'n_clicks'),
            Input('exportFilename', 'value') ,
            prevent_initial_call=True,)
        def saveData(n_clicks, filename):
            if n_clicks>0:
                self.pandasData.to_csv(filename)
                
        @app.callback(
            Input('channel', 'value'),
            )
        def changeChannels(channels):
            self.selectedChannels = channels
        
        @app.callback(
            Output('channel','value',allow_duplicate=True),
            Input ('shorted', 'n_clicks'), 
            prevent_initial_call=True,)
        def shorted(n_clicks):
            return self.findShortedChannels()
            
    def appPlot(self,app):
        
        @app.callback(
            Input('widthSlider', 'value'))
        def resize_figure(width):
            self.graphWidth = int( 1400/100* width)
    
        @app.callback(            Input('logDD', 'value'))
        def changeLog(logDD):
            self.logPlot= (logDD== 'log')
            
        @app.callback(
            Input('timeDD', 'value'),
            )
        def changeTime(timeDD):
            self.timeMode = timeDD
        
        @app.callback(
            Output('graphs','children', allow_duplicate=True),
            Output('bSingles','n_clicks'),
            Input('bSingles', 'n_clicks'), 
            prevent_initial_call=True,
            running=[(Output("graphSpacer", "children"), 'Loading', ' - ')])
        def plotSingles(single_clicks ):
            if single_clicks>0:
                fig= self.Plot('Singles')
                return fig,0
            return [],0
            
        @app.callback(
            Output('graphs','children', allow_duplicate=True),
            Output('bEnsemble','n_clicks'),
            Input('bEnsemble', 'n_clicks'), 
            prevent_initial_call=True,
            running=[(Output("graphSpacer", "children"), 'Loading', ' - ')])
        def plotEnsemble( ensemble_clicks ):
            if ensemble_clicks>0:
                figs= self.Plot('Ensemble')
                return figs,0
            else:
                return [],0
            
        @app.callback(
            Output('graphs','children', allow_duplicate=True),
            Output('bViolin','n_clicks'),
            Input('bViolin', 'n_clicks'), 
            prevent_initial_call=True,
            running=[(Output("graphSpacer", "children"), 'Loading', ' - ')])
        def plotViolin( ensemble_clicks ):
            if ensemble_clicks>0:
                figs= self.Plot('Violin')
                return figs,0
            else:
                return [],0
            
        @app.callback(
            Input('bCapacitance', 'value'))
        def change_iv(title):
            if (title=='Conductance'):
                self.IVMode ='conductance'
            else:
                self.IVMode ='capacitance'
                
            
        self.VoltageCallbacks(app)
        
    def CreateIVDash(self):
        external_stylesheets = ['https://codepen.io/chriddyp/pen/bWLwgP.css']

        app = Dash(__name__, external_stylesheets=external_stylesheets)
        app.config.suppress_callback_exceptions = True
        app.layout = self.CreateLayout()
        
        self.appChipWafer(app)
        self.appDataType(app)
        self.appPlot(app)
        
        app.run_server( debug=True)    
        
    def VoltageCallbacks(self,app):
        
        @app.callback(Input('volts', 'value'))
        def allVolts( value ):
            if value=='All':
                self.VoltSelectionIndex = -1
            else:
                self.VoltSelectionIndex = self.VoltSelection.index(float(value.split(' ')[0])/1000)
             
        
        @app.callback(Input('slew', 'value'))
        def allSlews( ensemble_clicks ):
            if ensemble_clicks=='All':
                self.ScanSelectionIndex = -1
            else:
                self.ScanSelectionIndex = self.ScanSelection.index(float(ensemble_clicks.split(' ')[0])/1000)
    
    def LoadDatas(self,selectedAnalytes,voltages = [], simplifyIV=True):
        traces=[]
        firstTime =0
        for analyte in selectedAnalytes:
            channelNames = [] 
            channelNames .extend( [ x.split('C-')[1].split('_')[0] for x in analyte['files'] if 'C-' in x])
            channelNames=sorted(list(set(channelNames)))
            for channelName in channelNames:
                files = [ f"{analyte['folder']}/{x}" for x in analyte['files'] if f'C-{channelName}_' in x]
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
                        if fileInfo['fileType']=='IV' or fileInfo['fileType']=='LV':
                            if simplifyIV==False:
                                fileInfo['current']=fileInfo['mean_data']
                                fileInfo['voltage']=fileInfo['bias']
                                
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
    
    def LoadDataType(self):
        if self.dataType=='':
            return []
        
        traceFingerPrints = self.wafer+self.chip+self.dataType
        
        if self.traceFingerPrints!=traceFingerPrints:
            selectedAnalytes = SelectData( self.analytes,  type=self.dataType)   
            self.allTraces,analyteColors=self.LoadDatas(selectedAnalytes,voltages = [], simplifyIV=False)
            
            #if otherTraces:
            #self.allTraces.extend(otherTraces)
            self.traceFingerPrints = traceFingerPrints
            
            self.VoltSelectionIndex = -1
            self.ScanSelectionIndex = -1
        buttons=[]
        if 'max_bias' in  self.allTraces[0]:
            self.VoltSelection=  list(set([np.round(100*x['max_bias'])/100 for x in self.allTraces]))
        else:
            self.VoltSelection=  list(set([np.round(100*x['bias'])/100 for x in self.allTraces]))
         
        volts = [str(x*1000) + ' mV' for x in self.VoltSelection]
        volts.append('All')

        if len(volts)==2:
            buttons=[]
        else:
            buttons =  [ html.Div([('Selected Max Voltage:'), dcc.RadioItems(
                id='volts', 
                options=volts,
                value='All',
                inline=True, 
                style={"color": "white"}
            )], style={"color": "white"})]
        
        slewButtons = [] 
        if 'slew' in  self.allTraces[0]:
            self.ScanSelection=  list(set([int(1000*x['slew'])/1000 for x in self.allTraces]))
            slewButtons = [ str(x*1000) + ' mV/s' for x in self.ScanSelection]
            slewButtons.append('All')
                
            if len(slewButtons)==2:
                slewButtons=[]
            else:
                slewButtons = [ html.Div([('Selected Slew:'), dcc.RadioItems(
                    id='slew', 
                    options=slewButtons,
                    value='All',
                    inline=True
                )], style={"color": "white"})]
        
        
        return buttons,slewButtons
    def Plot(self, graphType):
        fig=[]

        if graphType =='Singles' : 
            fig= self.PlotSingle( )
        elif graphType =='Ensemble':
            fig = self.PlotEnsemble( )
            fig= [dcc.Graph(figure=fig)]
        elif graphType =='Violin':
            fig= self.PlotViolin(  )
        
        return fig
            
    def findShortedChannels(self):
        traces = [x for x in self. allTraces if x['shortAnalyte'] in self.selectedAnalytes]
        traces = sorted(traces, key=lambda x: x['fileTime'])
        goodChannels = [] 
        for channel in self.selectedChannels:
            dataPoints = [x for x in traces if x['channel']==channel]
            if len(dataPoints)==0:
                continue
            data=(dataPoints[0])
            if data['fileType']=='IV':
                current = data['current']
            else:
                current = data['mean_data']
            try:
                if len(current)>0 and np.max(current)<13:
                    goodChannels.append(channel)
            except:
                if np.abs(current)<13:
                    goodChannels.append(channel)
        return goodChannels
        
    def PlotSingle(self):
        figs = []
        
        self.graphMode = 'Singles'
        for channel in self.selectedChannels:
            if self.dataType == 'DL':
                fig = self._PlotDL(channel,self.selectedAnalytes)
            elif self.dataType == 'IV':
                fig = self._PlotIV(channel,self.selectedAnalytes)
            elif self.dataType == 'RT':
                fig = self._PlotRT(channel,self.selectedAnalytes)
            figs.append(dcc.Graph(figure=fig))
        
        return figs 
    def PlotViolin(self ):
        self.graphMode = 'Violin'
        src = self.PlotEnsembleRTViolins( )
        return [html.Img(id='matplotlib', src=src)] 
    def PlotEnsemble(self):
        self.graphMode = 'Ensemble'
        if self.dataType == 'DL':
            fig = self.PlotEnsembleDLsPL()
        elif self.dataType == 'IV' or self.dataType == 'LV':
            fig = self.PlotEnsembleIVsPL()
        elif self.dataType == 'RT':
            fig = self.PlotEnsembleRTsPL( )
        return fig 
    def PlotEnsembleIVsPL(self):
        traces = [x for x in self. allTraces if x['fileType']=='IV' or x['fileType']=='LV']
        traces = [x for x in traces if x['shortAnalyte'] in self.selectedAnalytes]
        if self.VoltSelectionIndex!=-1:
            targetBias = self.VoltSelection[self.VoltSelectionIndex]
            traces = [x for x in traces if np.abs(x['max_bias']-targetBias)<.03]
            
        if self.ScanSelectionIndex!=-1:
            targetSlew = self.ScanSelection[self.ScanSelectionIndex]
            traces = [x for x in traces if np.abs(x['slew']-targetSlew)<.03]            
        #sort traces by time
        traces = sorted(traces, key=lambda x: x['fileTime'])

        channelNames = self.selectedChannels
        channelNames = sorted(channelNames)
    
        tranceAnalytes= np.unique([x['shortAnalyte'] for x in traces])
        traceTimes = {}
        for analyte in tranceAnalytes:
            dataPoints = [x for x in traces if x['shortAnalyte']==analyte]
            traceTimes[analyte] = np.min( [x['fileTime'] for x in dataPoints] )
        #sort traceanalytes by tracetimes
        tranceAnalytes = sorted(tranceAnalytes, key=lambda x: traceTimes[x])
        
        reverseLogTime= self.timeMode=='Reverse Log'
        logPlot = self.logPlot
        fig = go.Figure()
        cc=0
        maxConductance =0
        indexedData = {}
        for channelName in channelNames:
            times = []
            steps = [] 
            conductances = []
            analyteTimes = []
            analyteNames = []
            step =0 
            for analyte in tranceAnalytes:
                dataPoints = [x for x in traces if x['channel']==channelName and x['shortAnalyte']==analyte]
                dataPoints = sorted( dataPoints, key=lambda x: x['fileTime'])
               
                conductance = [ x[self.IVMode]  for x in dataPoints]
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
                
                if self.timeMode =='Step':
                    times.append(np.max(combinedTime))
                    conductances.append(np.max(combinedConductance))
                    steps.append( step )
                else :
                    times.extend(combinedTime)
                    steps.extend( step + np.linspace(0,1,len(combinedTime)))
                    conductances.extend(combinedConductance)
                    
                step +=1
                if cc==0:
                    analyteTimes.append(time_Hrs[0])
                    analyteNames.append(analyte.strip('_'))
            
            if self.timeMode =='Step':
                if 'time' not in indexedData:
                    indexedData['time'] = steps
                indexedData[channelName] = np.array(conductances)*1e-9
                fig.add_trace(go.Scatter(x=steps, y=indexedData[channelName] , mode='lines+markers', name=channelName.strip('_')))
                maxConductance= np.max([maxConductance,np.max(conductances)])
            else:
                indx= np.argsort(times)
                times = np.array(times)[indx]
                maxTime = times[-1]
                if reverseLogTime:
                    times = np.log(maxTime -times +.1)*-1
                    analyteTimes = np.log( maxTime- np.array(analyteTimes)+.1)*-1
                    
                conductances = np.array(conductances)[indx]
                if 'time' not in indexedData:
                    indexedData['time'] = times
                indexedData[channelName] = np.array(conductances)*1e-9
                fig.add_trace(go.Scatter(x=times, y=indexedData[channelName], mode='lines+markers', name=channelName.strip('_')))
                for time,name in zip(analyteTimes,analyteNames):
                    fig.add_trace(go.Scatter(x=[time,time], y=[1e-12,200*1e-9], text=name.strip('_'), mode='lines',line=dict(color="#000000", width=5)))
                    y=100*1e-9
                    fig.add_annotation(x=time, y=y,textangle=90, text= name.strip('_'), showarrow=True, yshift=-100)
            cc+=1                
        self.pandasData = pd.DataFrame(indexedData)
        if logPlot:
            fig.update_yaxes(type="log")
            
        if self.timeMode =='Step':
            xlabel = 'Analyte'
            #create a list of steps at the top of the graph with the analyte names at each integer
            for i,analyte in enumerate(tranceAnalytes):
                fig.add_annotation(x=i, y=maxConductance*1e-9,text=analyte.strip('_') )
            
        elif reverseLogTime:
            xlabel = '-1*Log Measurement Time (hrs)'
        else:
            xlabel = 'Measurement Time (hrs)'
            
        if self.IVMode=='conductance':
            ylabel = 'Conductance (S)'
            title = f"Conductance"
        else:
            ylabel = 'Capacitance (F)'
            title = f"Capacitance"
        fig.update_layout(
            title=title, 
            xaxis_title=xlabel,
            yaxis_title=ylabel,
            legend_title="Channels",
            width=self.graphWidth,
            paper_bgcolor=self.backColor,
            plot_bgcolor=self.backColor,
            font=dict(
                family="Courier New, monospace",
                size=15,
                color="Black"
            )
        )
        
        fig.update_xaxes(showline=True, linewidth=2, linecolor='black',showgrid=True, gridwidth=1, gridcolor='grey')
        fig.update_yaxes(showline=True, linewidth=2, linecolor='black',showgrid=True, gridwidth=1, gridcolor='grey')

        return fig
    def PlotEnsembleDLsPL(self, dataType='mean', selectedBias=0):
        traces = [x for x in self.allTraces if x['fileType']=='DL' ]
        #filter the ones that are not in the selected analytes
        traces = [x for x in traces if x['shortAnalyte'] in self.selectedAnalytes]
        if self.VoltSelectionIndex!=-1:
            targetBias = self.VoltSelection[self.VoltSelectionIndex]
            traces = [x for x in traces if np.abs(x['bias']-targetBias)<.03]
        
        voltsUsed = np.unique([x['bias'] for x in traces])
        if len(voltsUsed)>1:
            useConductance = True
        else:
            useConductance = False
        
        #sort traces by time
        traces = sorted(traces, key=lambda x: x['fileTime'])
        channelNames = self.selectedChannels
        channelNames = sorted(channelNames)
        reverseLogTime= self.timeMode=='Reverse Log'
        
        
        tranceAnalytes= np.unique([x['shortAnalyte'] for x in traces])
        traceTimes = {}
        
        for analyte in tranceAnalytes:
            dataPoints = [x for x in traces if x['shortAnalyte']==analyte]
            traceTimes[analyte] = np.min( [x['fileTime'] for x in dataPoints] )
             
            
        #sort traceanalytes by tracetimes
        tranceAnalytes = sorted(tranceAnalytes, key=lambda x: traceTimes[x])
        
        
        
        cc=0
        fig = go.Figure()
        maxConductance =0
        minConductance = 1e12
        maxTime = 0
        plotTimes = {}
        indexedData = {}
        for channelName in channelNames:
            dataPoints = [x for x in traces if x['channel']==channelName  ] 
            
            lastTime = 0
            times =[]
            steps = []
            datas =[]
            
            step =0 
            for analyte in tranceAnalytes:
                dataPoints = [x for x in traces if x['channel']==channelName and x['shortAnalyte']==analyte]
                dataPoints = sorted( dataPoints, key=lambda x: x['fileTime'])
                analyteTimes =[]
                analyteData =[]
                for trace in dataPoints:
                    bias = trace['bias']
                    
                    if bias<0 and dataType=='max':
                        tDataType='min_data'
                    elif bias>0 and dataType=='min':
                        tDataType='max_data'
                    else:
                        tDataType=dataType+ '_data'
                    current =  trace[tDataType ]  
                    time_Hrs = lastTime + np.linspace(0,len(current) * trace['sampleRate'],len(current))/60/60
                    
                    analyteTimes.extend(time_Hrs)
                     
                    if useConductance:
                        yData = np.abs( current/ bias)
                    else:
                        yData=np.abs( current)
                    maxConductance = np.max([maxConductance,np.max(yData)])
                    minConductance = np.min([minConductance,np.min(yData)])
                    analyteData.extend(yData)
                    lastTime = time_Hrs[-1]
                if cc==0:
                    plotTimes[analyte]=analyteTimes[0]
                    
                times.extend(analyteTimes)
                datas.extend(analyteData)
                steps.extend( step + np.linspace(0,1,len(analyteTimes)))
                step+=1
                
            if len(times)>100000:
                times = decimate(times,10)
                datas = decimate(datas,10)
                    
            if self.timeMode =='Step':
                if 'time' not in indexedData:
                    indexedData['time'] = steps
                indexedData[channelName] = np.array(datas)*1e-9
                fig.add_trace(go.Scatter(x=steps, y=indexedData[channelName], mode='lines', name=channelName.strip('_')))
            else:
                if reverseLogTime:
                    maxTime = times[-1]
                    times = np.log(maxTime -times +.1)*-1
                if 'time' not in indexedData:
                    indexedData['time'] = times
                indexedData[channelName] = np.array(datas)*1e-9
                fig.add_trace(go.Scatter(x=times, y=indexedData[channelName], mode='lines', name=channelName.strip('_')))
            cc+=1
        self.pandasData = pd.DataFrame(indexedData)
        if self.timeMode!='Step':
            if reverseLogTime:
                xlabel = '-1*Log Measurement Time (hrs)'
            else:
                xlabel = 'Measurement Time (hrs)'
            for i,analyte in enumerate(tranceAnalytes):
                if self.timeMode=='Reverse Log':
                    firstTime = np.log(maxTime -plotTimes[analyte]+.1)*-1
                else:
                    firstTime = plotTimes[analyte]
                
                fig.add_trace(go.Scatter(x=[firstTime,firstTime], y=[minConductance*1e-9,maxConductance*1e-9], name=analyte.strip('_'), mode='lines',line=dict(color="#000000", width=5)))
                fig.add_annotation(x=firstTime, y=maxConductance*1e-9,textangle=90, text=analyte.strip('_') )
        else:
            xlabel = 'Analyte'            
             #create a list of steps at the top of the graph with the analyte names at each integer
            for i,analyte in enumerate(tranceAnalytes):
                fig.add_annotation(x=i, y=maxConductance*1e-9,text=analyte.strip('_') )
                
        if self.logPlot:
            fig.update_yaxes(type="log")
            
        if self.VoltSelectionIndex!=-1:
            title = f"DL @ {self.VoltSelection[self.VoltSelectionIndex]} V"
        else:
            title = f"DL"
        
        if useConductance:
            ylabel = f"Conductance ({dataType}) (S)"
        else:    
            ylabel = f"Abs Current ({dataType}) (A)"
        
        fig.update_layout(
            title=title, 
            xaxis_title=xlabel,
            yaxis_title=ylabel,
            legend_title="Channels",
            width=self.graphWidth,
            paper_bgcolor=self.backColor,
             plot_bgcolor=self.backColor,
            font=dict(
                family="Courier New, monospace",
                size=15,
                color="Black"
            )
        )
        
        fig.update_xaxes(showline=True, linewidth=2, linecolor='black',showgrid=True, gridwidth=1, gridcolor='grey')
        fig.update_yaxes(showline=True, linewidth=2, linecolor='black',showgrid=True, gridwidth=1, gridcolor='grey')
        return fig   
      
    def PlotEnsembleRTsPL(self):
        traces = [x for x in self.allTraces if x['fileType']=='RT' ]
        #sort traces by time
        traces = sorted(self.allTraces, key=lambda x: x['fileTime'])
        traces = [x for x in traces if x['shortAnalyte'] in self.selectedAnalytes]
       
        if self.VoltSelectionIndex!=-1:
            targetBias = self.VoltSelection[self.VoltSelectionIndex]
            traces = [x for x in traces if np.abs(x['bias']-targetBias)<.03]
            
        voltsUsed = np.unique([x['bias'] for x in traces])
        if len(voltsUsed)>1:
            useConductance = True
        else:
            useConductance = False
        
            
        tranceAnalytes= np.unique([x['shortAnalyte'] for x in traces])
        traceTimes = {}
        
        for analyte in tranceAnalytes:
            dataPoints = [x for x in traces if x['shortAnalyte']==analyte]
            traceTimes[analyte] = np.min( [x['fileTime'] for x in dataPoints] )
            
        #sort traceanalytes by tracetimes
        tranceAnalytes = sorted(tranceAnalytes, key=lambda x: traceTimes[x])
        
        reverseLogTime= self.timeMode=='Reverse Log'
        fig = go.Figure()
        maxConductance =0
        minConductance = 1e12
        maxTime = 0
        plotTimes = {}
        cc=0
        indexedData = {}
        for channelName in self.selectedChannels:
            currents = []
            times = []
            steps = []
            lastTime = 0
            step =0 
            for analyte in tranceAnalytes:
                dataPoints = [x for x in traces if x['channel']==channelName and x['shortAnalyte']==analyte]
                dataPoints = sorted( dataPoints, key=lambda x: x['fileTime'])
            
                analyteTimes =[]
                analyteData =[]
                for dataPoint in dataPoints:
                    bias = dataPoint['bias']
                    duration = dataPoint['measureTime']/60/60
                    current = decimate( dataPoint['mean_data'],10)
                    current = savgol_filter(current, 11, 3)
                    analyteTimes.extend(lastTime+ np.linspace(0,duration,len(current)))
                    if useConductance:
                        yData = np.abs( current/ bias)
                    else:
                        yData=np.abs( current)
                    maxConductance = np.max([maxConductance,np.max(yData)])
                    minConductance = np.min([minConductance,np.min(yData)])
                    analyteData.extend(yData)
                    lastTime=analyteTimes[-1]
                    
                
                if cc==0:
                    plotTimes[analyte]=analyteTimes[0]
                    
                times.extend(analyteTimes)
                currents.extend(analyteData)
                steps.extend( step + np.linspace(0,1,len(analyteTimes)))
                step+=1
                
            if self.logPlot:
                currents =  (np.abs(currents)+1e-15)
                
            if self.timeMode =='Step':
                if 'time' not in indexedData:
                    indexedData['time'] = steps
                indexedData[channelName] =np.array(currents)*1e-9
                fig.add_trace(go.Scatter(x=steps, y=indexedData[channelName], mode='lines', name=channelName.strip('_')))
            else:
                if reverseLogTime:
                    maxTime = times[-1]
                    times = np.log(maxTime -times +.1)*-1
                if 'time' not in indexedData:
                    indexedData['time'] = times
                indexedData[channelName] =np.array(currents)*1e-9
                fig.add_trace(go.Scatter(x=times, y=indexedData[channelName], mode='lines', name=channelName.strip('_')))
            cc+=1
        self.pandasData = pd.DataFrame(indexedData)
        if self.timeMode!='Step':
            if reverseLogTime:
                xlabel = '-1*Log Measurement Time (hrs)'
            else:
                xlabel = 'Measurement Time (hrs)'
            for i,analyte in enumerate(tranceAnalytes):
                if self.timeMode=='Reverse Log':
                    firstTime = np.log(maxTime -plotTimes[analyte]+.1)*-1
                else:
                    firstTime = plotTimes[analyte]
                
                fig.add_trace(go.Scatter(x=[firstTime,firstTime], y=[minConductance*1e-9,maxConductance*1e-9], name=analyte.strip('_'), mode='lines',line=dict(color="#000000", width=5)))
                fig.add_annotation(x=firstTime, y=maxConductance*1e-9,textangle=90, text=analyte.strip('_') )
        else:
            xlabel = 'Analyte'            
            #create a list of steps at the top of the graph with the analyte names at each integer
            for i,analyte in enumerate(tranceAnalytes):
                fig.add_annotation(x=i, y=maxConductance*1e-9,text=analyte.strip('_') )
           
            
        if self.logPlot:
            fig.update_yaxes(type="log")
        
        if useConductance:
            ylabel = f"Conductance (S)"
        else:    
            ylabel = f"Current (A)"

            
        fig.update_layout(
            title=f"RTs", 
            xaxis_title=xlabel,
            yaxis_title=ylabel,
            legend_title="Analytes",
            width=self.graphWidth,
            paper_bgcolor=self.backColor,
             plot_bgcolor=self.backColor,
            font=dict(
                family="Courier New, monospace",
                size=15,
                color="Black"
            )
        )
        
        fig.update_xaxes(showline=True, linewidth=2, linecolor='black',showgrid=True, gridwidth=1, gridcolor='grey')
        fig.update_yaxes(showline=True, linewidth=2, linecolor='black',showgrid=True, gridwidth=1, gridcolor='grey')

        return fig 
    def PlotEnsembleRTViolins(self):

        traces = [x for x in self.allTraces if x['fileType']=='RT']
        
        if self.VoltSelectionIndex!=-1:
            targetBias = self.VoltSelection[self.VoltSelectionIndex]
            traces = [x for x in traces if np.abs(x['bias']-targetBias)<.03]
        #sort traces by time
        traces = sorted(traces, key=lambda x: x['fileTime'])
        #check for shorted channels
        channelNames =  self.selectedChannels
        folderPile = {}
        startCurrent ={}
        xlabels =[]
        
        
        for analyte in self.selectedAnalytes:
            dataPile = []
            
            
            dataPoints = [x for x in traces if x['shortAnalyte']==analyte  ]
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
                
                if (self.logPlot):
                    current = np.log( np.abs( current)+1e-9)
                
                dataPile.extend(np.array(current))
                
            if len(dataPile)>0:
                xlabels.append(analyte.strip('_').strip())
                folderPile[analyte.strip('_').strip()]= (dataPile)
                #aColor[analyte.strip('_').strip()]=analyteColors[ analyte][-1]
            
                
        fig=plt.figure(figsize=(7,4))
        ax=sns.violinplot(data=folderPile,   density_norm='width', inner='point',cut=0, linewidth=1) # palette=aColor, 
        #ax.set_xticklabels(xlabels)
        if self.logPlot:
            plt.ylabel('Log(Delta Current (nA))')
        else:
            plt.ylabel('Delta Current (nA)')
            
        plt.title(f'RT ')
        plt.xticks(rotation=45)
        plt.tight_layout()
        buf = BytesIO()
        fig.savefig(buf, format="png",dpi=300, bbox_inches = "tight")
        # Embed the result in the html output.
        fig_data = base64.b64encode(buf.getbuffer()).decode("ascii")
        return f'data:image/png;base64,{fig_data}'     
    def _PlotRT(self,channelNames,tranceAnalytes):
       
        traces = [x for x in self.allTraces if x['fileType']=='RT' and x['channel'] in channelNames and x['shortAnalyte'] in tranceAnalytes  ]
        
        
        if self.VoltSelectionIndex!=-1:
            targetBias = self.VoltSelection[self.VoltSelectionIndex]
            traces = [x for x in traces if np.abs(x['bias']-targetBias)<.03]
            
        #sort traces by time
        dataPoints = sorted(traces, key=lambda x: x['fileTime'])
       
        fig = go.Figure()
        lastTime = 0
        reverseLogTime= self.timeMode=='Reverse Log'
        analytes = {} 
        maxTime = 0
        for dataPoint in dataPoints:
            analyte = dataPoint['shortAnalyte']
            if analyte not in analytes:
                analytes[analyte]= { 'times':[], 'currents':[]}
                
            duration = dataPoint['measureTime']/60/60
            current = decimate( dataPoint['mean_data'],10)
            times = lastTime+ np.linspace(0,duration,len(current))
            analytes[analyte]['times'] .extend(times)
            analytes[analyte]['currents'].extend(current)
            lastTime=times[-1]
            if lastTime>maxTime:
                maxTime = lastTime
            
        
              
            
        for analyte in analytes:
            times = analytes[analyte]['times']
            currents = analytes[analyte]['currents']
            if self.logPlot:
                currents =  (np.abs(currents)+1e-15)
            if reverseLogTime:
                times = np.log(maxTime -times +.1)*-1
            fig.add_trace(go.Scatter(x=times, y=np.array(currents)*1e-9, mode='lines', name=analyte.strip('_')))
        
        if self.logPlot:
            fig.update_yaxes(type="log")
        
        if reverseLogTime:
            xlabel = '-1*Log Measurement Time (hrs)'
        else:
            xlabel = 'Measurement Time (hrs)'
            
        fig.update_layout(
            title=f"Channel {channelNames} RT", 
            xaxis_title=xlabel,
            yaxis_title="Current (A)",
            legend_title="Analytes",
            width=self.graphWidth,
            paper_bgcolor=self.backColor,
             plot_bgcolor=self.backColor,
            font=dict(
                family="Courier New, monospace",
                size=15,
                color="Black"
            )
        )
        
        fig.update_xaxes(showline=True, linewidth=2, linecolor='black',showgrid=True, gridwidth=1, gridcolor='grey')
        fig.update_yaxes(showline=True, linewidth=2, linecolor='black',showgrid=True, gridwidth=1, gridcolor='grey')

        return fig 
    def _PlotDL(self,channelNames,tranceAnalytes):
        traces = [x for x in self.allTraces if x['fileType']=='DL']
        traces = [x for x in traces if x['shortAnalyte'] in self.selectedAnalytes]
        if self.VoltSelectionIndex!=-1:
            targetBias = self.VoltSelection[self.VoltSelectionIndex]
            traces = [x for x in traces if np.abs(x['bias']-targetBias)<.03]
            
        #sort traces by time
        traces = sorted(traces, key=lambda x: x['fileTime'])
       
        orderedAnalytes = []
        for trace in traces:
            if trace['shortAnalyte'] not in orderedAnalytes and trace['shortAnalyte'] in tranceAnalytes:
                orderedAnalytes.append(trace['shortAnalyte'])
        reverseLogTime= self.timeMode=='Reverse Log'
        fig = go.Figure()
        if reverseLogTime:
            lastTime = 0
            for analyte in orderedAnalytes:
                dataPoints = [x for x in traces if x['channel'] in channelNames and x['shortAnalyte']==analyte]
                times = []
                for dataPoint in dataPoints:
                    duration = dataPoint['measureTime']/60/60
                    current = dataPoint['mean_data']
                    times.extend(lastTime+ np.linspace(0,duration,len(current)))
                    lastTime=times[-1]
                maxTime = times[-1]
        
        lastTime = 0
        for analyte in orderedAnalytes:
            dataPoints = [x for x in traces if x['channel'] in channelNames and x['shortAnalyte']==analyte]
            currents = []
            times = []
            
            for dataPoint in dataPoints:
                duration = dataPoint['measureTime']/60/60
                current = dataPoint['mean_data']
                times.extend(lastTime+ np.linspace(0,duration,len(current)))
                currents.extend(current)
                lastTime=times[-1]
                
            if self.logPlot:
                currents =  (np.abs(currents)+1e-15)
                
            if reverseLogTime:
                times = np.log(maxTime -times +.1)*-1
                
            fig.add_trace(go.Scatter(x=times, y=np.array(currents)*1e-9, mode='lines', name=analyte.strip('_')))
        
        if self.logPlot:
            fig.update_yaxes(type="log")
        
        if reverseLogTime:
            xlabel = '-1*Log Measurement Time (hrs)'
        else:
            xlabel = 'Measurement Time (hrs)'
            
        fig.update_layout(
            title=f"Channel {channelNames} IV", 
            xaxis_title=xlabel,
            yaxis_title="Current (A)",
            legend_title="Analytes",
            width=self.graphWidth,
            paper_bgcolor=self.backColor,
             plot_bgcolor=self.backColor,
            font=dict(
                family="Courier New, monospace",
                size=15,
                color="Black"
            )
        )
        fig.update_xaxes(showline=True, linewidth=2, linecolor='black',showgrid=True, gridwidth=1, gridcolor='grey')
        fig.update_yaxes(showline=True, linewidth=2, linecolor='black',showgrid=True, gridwidth=1, gridcolor='grey')

        return fig 
    def _PlotIV(self,channelNames,tranceAnalytes ):
        traces = [x for x in self.allTraces if x['fileType']=='IV']
        traces = [x for x in traces if x['shortAnalyte'] in self.selectedAnalytes]
        #sort traces by time
        traces = sorted(traces, key=lambda x: x['fileTime'])
        
        
        
        if self.VoltSelectionIndex!=-1:
            targetBias = self.VoltSelection[self.VoltSelectionIndex]
            traces = [x for x in traces if np.abs(x['max_bias']-targetBias)<.03]
            
        if self.ScanSelectionIndex!=-1:
            targetSlew = self.ScanSelection[self.ScanSelectionIndex]
            traces = [x for x in traces if np.abs(x['slew']-targetSlew)<.03]   
       
        fig = go.Figure()
        for analyte in tranceAnalytes:
            dataPoints = [x for x in traces if x['channel'] in channelNames and x['shortAnalyte']==analyte]
            if len(dataPoints)>0:
                volts = dataPoints[-1]['voltage']
                currents = dataPoints[-1]['current']
                if self.logPlot:
                    currents =  (np.abs(currents)+1e-15)
                fig.add_trace(go.Scatter(x=volts, y=currents*1e-9, mode='lines', name=analyte.strip('_')))
        
        if self.logPlot:
            fig.update_yaxes(type="log")
      
            #make the plot space white 
        fig.update_layout(
            title=f"Channel {channelNames} IV", 
            xaxis_title='Volts (V)',
            yaxis_title="Current (A)",
            legend_title="Analytes",
            width=self.graphWidth,
            paper_bgcolor=self.backColor,
            plot_bgcolor=self.backColor,
            font=dict(
                family="Courier New, monospace",
                size=15,
                color="Black"
            )
        )
        
        fig.update_xaxes(showline=True, linewidth=2, linecolor='black',showgrid=True, gridwidth=1, gridcolor='grey')
        fig.update_yaxes(showline=True, linewidth=2, linecolor='black',showgrid=True, gridwidth=1, gridcolor='grey')

        return fig 
    
    
   