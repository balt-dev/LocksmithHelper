local drawableSprite = require("structs.drawable_sprite")
local drawableFunction = require("structs.drawable_function")
local drawing = require("utils.drawing")

local colorTable = {
    White =     {0xeb / 0xff, 0xe8 / 0xff, 0xe4 / 0xff},
    Orange =    {0xdc / 0xff, 0x8c / 0xff, 0x32 / 0xff},
    Purple =    {0xaa / 0xff, 0x50 / 0xff, 0xc8 / 0xff},
    Red =       {0xc8 / 0xff, 0x37 / 0xff, 0x37 / 0xff},
    Green =     {0x35 / 0xff, 0x9f / 0xff, 0x50 / 0xff},
    Blue =      {0x5f / 0xff, 0x71 / 0xff, 0xa0 / 0xff},
    Pink =      {0xcf / 0xff, 0x70 / 0xff, 0x9f / 0xff},
    Cyan =      {0x50 / 0xff, 0xaf / 0xff, 0xaf / 0xff},
    Black =     {0x36 / 0xff, 0x30 / 0xff, 0x29 / 0xff},
    Brown =     {0xaa / 0xff, 0x60 / 0xff, 0x15 / 0xff},
    Glitch =    {0x60 / 0xff, 0x60 / 0xff, 0x60 / 0xff},
    Master =    {0xeb / 0xff, 0xdd / 0xff, 0x5e / 0xff},
    Pure =      {0xe6 / 0xff, 0xee / 0xff, 0xf3 / 0xff},
    Stone =     {0x80 / 0xff, 0x88 / 0xff, 0x90 / 0xff},
}

local colors = {}
for key, _ in pairs(colorTable) do
    colors[#colors+1] = key
end

local function validateComplex(str)
    return (str:find("^[-%+]?%d+$") or
        str:find("^[-%+]?%d*i$") or
        str:find("^[-%+]?%d+[-%+]%d*i$")) ~= nil
end

return {
    name = "LocksmithHelper/Key",
    depth = 0,
    sprite = function(room, entity)
        return drawableFunction.fromFunction(function()
            local textureName
            if (entity.type == "Add") or (entity.type == "Multiply") then
                if entity.color == "Master" then
                    textureName = "master"
                else
                    textureName = "normal"
                end
            elseif entity.type == "Star" then
                textureName = "star"
            elseif entity.type == "Unstar" then
                textureName = "unstar"
            else
                textureName = "set"
            end

            local texture = drawableSprite.fromTexture("objects/LocksmithHelper/key/" .. textureName, entity)
            local outline = drawableSprite.fromTexture("objects/LocksmithHelper/key/" .. textureName .. "_outline", entity)    
            texture.color = colorTable[entity.keyColor] or {1, 0, 1};
            drawing.callKeepOriginalColor(function()
                love.graphics.setColor(1, 1, 1, 1)
                texture:draw()
                outline:draw()
                local printStr = entity.value
                if (entity.type == "Star" or entity.type == "Unstar") then
                    printStr = ""
                elseif (entity.type == "Multiply") then
                    printStr = "x" .. printStr
                end
                love.graphics.print(printStr, entity.x - (printStr:len() * 2) + 2, entity.y + 4)
            end)
        end)
    end,
    placements = {
        name = "key",
        data = {
            type = "Add",
            keyColor = "Orange",
            value = "1"
        },
    },
    fieldInformation = {
        keyColor = {
            options = colors
        },
        type = {
            options = {"Add", "Multiply", "Set", "Star", "Unstar"}
        },
        value = {
            validator = validateComplex
        }
    },
    selection = function(room, entity)
        return {x = entity.x - 8, y = entity.y - 8, width = 16, height = 16}
    end
}