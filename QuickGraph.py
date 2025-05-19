import time
import struct
import numpy as np
import win32ui
from subprocess import Popen, PIPE



class CGraph:
    def __init__(self,handle, quickGraphs, graphType):
        
        self.handle = handle
        self.quickGraphs = quickGraphs
        self.graphType = graphType
       
        
    def StreamData(self, data):
        data = np.ravel(np.array(data, dtype=float))
       
        if self.graphType=="DataStream":
            self.quickGraphs.SendString("StreamData")
        else:
            self.quickGraphs.SendString('SignalData')
           
        self.quickGraphs.SendString(self.handle)
        self.quickGraphs.SendDoubles(data)
        
    def StreamScatter(self, x, y, caption=""):
        x = np.ravel(np.array(x, dtype=float))
        y = np.ravel(np.array(y, dtype=float))
        if self.graphType=="MultiScatter":
            self.quickGraphs.SendString("MultiData")
            self.quickGraphs.SendString(self.handle)
            self.quickGraphs.SendString(caption)
        elif self.graphType=="Long":
            self.quickGraphs.SendString("LongData")
            self.quickGraphs.SendString(self.handle)
            self.quickGraphs.SendString(caption)
        else :
            self.quickGraphs.SendString("ScatterData")
            self.quickGraphs.SendString(self.handle)
        self.quickGraphs.SendDoubles(x)
        self.quickGraphs.SendDoubles(y)
        
    def ClearGraph(self):
        self.quickGraphs.SendString("ClearData")
        self.quickGraphs.SendString(self.handle)
        


class QuickGraphs:
    def __enter__(self):
        try :
            win32ui.FindWindow(None,"QUICKGRAPHS")
        except:
            process = Popen([r'C:\DempBot\DempBotPy\GraphServer\bin\Debug\GraphServer.exe'], stdout=PIPE, stderr=PIPE) 
            found =False
            while (found==False):
                time.sleep(.5)
                try:
                    win32ui.FindWindow(None,"QUICKGRAPHS")
                    found=True
                except:
                    pass

        
        self.f = open(r'\\.\pipe\NPtest', 'r+b', 0)        
        return self
    def Start(self):
        self.__enter__()
    def Close(self):
        self.__exit__(None,  None, None)

    def __exit__(self, exc_type, exc_val, exc_tb):
        self.f.close()
        
    def SendString(self,str):
        s=str.encode('ascii')
        self.f.write(struct.pack('I', len(s)) + s)   # Write str length and str
        self.f.seek(0)                               # EDIT: This is also necessary
    def ReadString(self):
        n = struct.unpack('I', self.f.read(4))[0]    # Read str length
        s = self.f.read(n).decode('ascii')           # Read str
        self.f.seek(0)                               # Important!!!        
        return s
    def SendDoubles(self,data):
        self.f.write( struct.pack('I', len(data)))
        self.f.seek(0)
        self.f.write( np.array( data  , dtype=float            ))
        self.f.seek(0)
        
    def CreateDataStreamGraph(self,title, xlabel,ylabel):
        self.SendString("AddGraph")
        self.SendString(title)
        self.SendString(xlabel)
        self.SendString(ylabel)
        self.SendString("DataStream")
        return CGraph(self.ReadString(),self,"DataStream")
        
    def CreateScatterGraph(self,title, xlabel,ylabel):
        self.SendString("AddGraph")
        self.SendString(title)
        self.SendString(xlabel)
        self.SendString(ylabel)
        self.SendString("Scatter")
        return CGraph(self.ReadString(),self,"Scatter")

    def CreateMultiScatterGraph(self,title, xlabel,ylabel):
        self.SendString("AddGraph")
        self.SendString(title)
        self.SendString(xlabel)
        self.SendString(ylabel)
        self.SendString("Long")
        return CGraph(self.ReadString(),self,"MultiScatter")
        
    def CreateLongGraph(self,title, xlabel,ylabel):
        self.SendString("AddLongGraph")
        self.SendString(title)
        self.SendString(xlabel)
        self.SendString(ylabel)
        self.SendString("Long")
        return CGraph(self.ReadString(),self,"Long")
        
    def CreateSignalGraph(self,title, xlabel,ylabel):
        self.SendString("AddGraph")
        self.SendString(title)
        self.SendString(xlabel)
        self.SendString(ylabel)
        self.SendString("Signal")
        return CGraph(self.ReadString(),self,"Signal")    

    def DeleteGraph(self,graph):
        self.SendString("DeleteGraph")
        self.SendString(graph.handle)
        del graph
        
    def DeleteAll(self):
        self.SendString("DeleteAll")

    def DeleteAllLong(self):
        self.SendString("DeleteAllLong")


