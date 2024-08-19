
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

local function parseRequirements(str)
    local t = {}
    for req in str:gmatch("([^,]+)") do
        local name, value, x, y, w, h
        _, _, name, value, x, y, w, h = req:find("^%s*(%w+)%s*:%s*(%S+)%s*@%s*(-?%d+)%s+(-?%d+)%s+(%d+)%s+(%d+)%s*$")
        if name == nil then
            _, _, name, value = req:find("^%s*(%w+)%s*:%s*(%S+)%s*$")
        end
        t[#t+1] = {color = name, value = value, x = x and tonumber(x), y = y and tonumber(y), w = w and tonumber(w), h = h and tonumber(h)}
    end
    return t
end

local cache = {}

return {
    name = "LocksmithHelper/Door",
    depth = 0,
    sprite = function(room, entity)
        local font = love.graphics.getFont()
        local reqTable = parseRequirements(entity.requirements)

        return drawableFunction.fromFunction(function()
            drawing.callKeepOriginalColor(function()
                love.graphics.setColor(colorTable[entity.spend] or {1, 0, 1})
                love.graphics.rectangle("fill", entity.x, entity.y, entity.width, entity.height)
                love.graphics.setColor(0, 0, 0)
                love.graphics.rectangle("line", entity.x, entity.y, entity.width, entity.height)
                for _, req in ipairs(reqTable) do
                    love.graphics.setColor(colorTable[req.color] or {1, 0, 1})
                    local x, y, w, h = req.x or 6, req.y or 6, req.w or entity.width - 12, req.h or entity.height - 12
                    love.graphics.rectangle("fill", entity.x + x, entity.y + y, w, h)
                    love.graphics.setColor(0, 0, 0)
                    love.graphics.rectangle("line", entity.x + x, entity.y + y, w, h)
                    if req.value ~= "blank" then
                        drawing.printCenteredText(req.value, entity.x + x, entity.y + y, w, h, font, 1)
                    end
                end

                if (entity.copies ~= "1") then
                    drawing.printCenteredText(entity.copies, entity.x, entity.y - 8, entity.width, 8, font, 1)
                end

                local effectString = ((entity.eroded and "E") or "")
                    .. ((entity.frozen and "F") or "")
                    .. ((entity.painted and "P") or "")
                    .. ((entity.cursed and "C") or "")
                love.graphics.print(effectString, entity.x, entity.y)
            end)
        end)
    end,
    placements = {
        name = "door",
        data = {
            eroded = false,
            frozen = false,
            painted = false,
            cursed = false,
            copies = "1",
            requirements = "",
            spend = "Orange",
            width = 16,
            height = 16
        },
    },
    fieldInformation = {
        copies = {
            validator = validateComplex
        },
        spend = {
            options = colors
        },
        requirements = {
            fieldType = "list",
            elementDefault = "Orange: 1",
            elementOptions = {
                validator = function(str)
                    local name, value, x, y, w, h
                    _, _, name, value, x, y, w, h = str:find("^%s*(%w+)%s*:%s*(%S+)%s*@%s*(-?%d+)%s+(-?%d+)%s+(%d+)%s+(%d+)%s*$")
                    if name == nil then
                        _, _, name, value = str:find("^%s*(%w+)%s*:%s*(%S+)%s*$")
                    end
                    if name == nil then
                        return false
                    end
                    return colorTable[name] ~= nil and (validateComplex(value) or value == "1x" or value == "-1x" or value == "ix" or value == "-ix" or value == "all" or value == "blank")
                end
            }
        }
    }
}