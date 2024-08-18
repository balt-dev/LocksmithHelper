return {
    name = "LocksmithHelper/Counter",
    depth = 0,
    texture = "objects/LocksmithHelper/counter",
    placements = { name = "counter" },
    selection = function(room, entity)
        return {x = entity.x - 8, y = entity.y - 8, width = 16, height = 16}
    end
}