
--  Data adapter
--  xlabel = Bias (mV)
--  ylabel = Current (nA)
--	xCol = 0
--  yCol = 1
--  dataStart = startIV
--  dataEnd = endIV
--  End Data adapter

--  Data adapter
--  xlabel = Time (s)
--  ylabel = Current (nA)
--	xCol = 0
--  yCol = 1
--  dataStart = startRT
--  dataEnd = endRT
--  End Data adapter


print('Hello World!')


reset()
smua.reset()


	local COMPLETE = "{COMPLETE}"
	-- Generate the source values
	local Vpp				= Vrms * math.sqrt(2)
	local sourceValues		= {} 
	local pointsPerCycle	= 7200 / frequency
	local numDataPoints		= 800
    local factor = 6.283185307/pointsPerCycle

    for i = 1, numDataPoints do
        sourceValues[i] = Vpp*   math.sin(math.pow(i,1.7)*factor )

    end

	-- Configure the SMU ranges
	smua.reset()
	smua.source.settling		= smua.SETTLE_FAST_POLARITY
	smua.source.autorangev		= smua.AUTORANGE_OFF
	smua.source.autorangei		= smua.AUTORANGE_OFF
    smua.source.rangev			= math.abs( Vpp) 
	smua.source.delay			= 0
	smua.source.limiti			= 1e-3

	smua.measure.autorangev		= smua.AUTORANGE_OFF
	smua.measure.autorangei		= smua.AUTORANGE_OFF
	smua.measure.autozero		= smua.AUTOZERO_OFF
	smua.measure.delay			= 0
	smua.measure.delayfactor    = 1
	smua.measure.analogfilter   = 0

	-- Voltage will be measured on the same range as the source range
	smua.measure.rangei			= limitI
	smua.measure.nplc			= 0.001

	-- Prepare the Reading Buffers
	smua.nvbuffer1.clear()
	smua.nvbuffer1.collecttimestamps	= 1
	smua.nvbuffer2.clear()
	smua.nvbuffer2.collecttimestamps	= 1

	-- Configure the trigger model
	--============================
	
	-- Timer 1 controls the time between source points
	trigger.timer[1].delay = (1 / 7200)
	trigger.timer[1].passthrough = true
	trigger.timer[1].stimulus = smua.trigger.ARMED_EVENT_ID
	trigger.timer[1].count = numDataPoints 

	-- Configure the SMU trigger model
	smua.trigger.source.listv(sourceValues)
	smua.trigger.source.limiti		= limitI
	smua.trigger.measure.action		= smua.ENABLE
	smua.trigger.measure.iv(smua.nvbuffer1, smua.nvbuffer2)
	smua.trigger.endpulse.action	= smua.SOURCE_HOLD
	smua.trigger.endsweep.action	= smua.SOURCE_IDLE
	smua.trigger.count				= numDataPoints
	smua.trigger.arm.stimulus		= 0
	smua.trigger.source.stimulus	= trigger.timer[1].EVENT_ID
	smua.trigger.measure.stimulus	= 0
	smua.trigger.endpulse.stimulus	= 0
	smua.trigger.source.action		= smua.ENABLE
	-- Ready to begin the test

	smua.source.output					= smua.OUTPUT_ON
	-- Start the trigger model execution
	smua.trigger.initiate()
	-- Wait until the sweep has completed
waitcomplete()
	smua.source.output					= smua.OUTPUT_OFF

    for x=1, smua.nvbuffer2.n do
	    print(smua.nvbuffer1.timestamps[x], smua.nvbuffer2[x], smua.nvbuffer1[x],0,0,0,0,0,0)
    end