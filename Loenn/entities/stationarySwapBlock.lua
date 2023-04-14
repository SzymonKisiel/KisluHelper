local stationarySwapBlock = {
    name = "KisluHelper/StationarySwapBlock",
    depth = -8500,
    placements = {
        {
            name = "stationary_swap_block",
            data = {
                sampleProperty = 0,
                width = 8,
                height = 8,
                activeOnStart = true
            },
        },
    },
}

stationarySwapBlock.fillColor = {0.3, 0.0, 0.7}
stationarySwapBlock.borderColor = {0.0, 0.0, 0.0}

return stationarySwapBlock