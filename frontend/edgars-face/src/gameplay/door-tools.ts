import type {OllamaFunctionDefinition, OllamaToolDefinition} from "../rest.api.ts";

export const toolDefinitions: OllamaToolDefinition[] = [{
    type: "function",
    function: {
        name: "list_doors",
        description: "Get all known doors on the ship and their status",
    } as OllamaFunctionDefinition
},
    {
        type: "function",
        function: {
            name: "open_door",
            description: "Opens doors based on door name provided and returns their status",
            parameters: {
                type: "object",
                properties: {
                    door_name: {
                        type: "string",
                        description: "Name of the door to open"
                    }
                }
            }
        }
    },
    {
        type: "function",
        function: {
            name: "close_door",
            description: "Closes doors based on door name provided and returns their status",
            parameters: {
                type: "object",
                properties: {
                    door_name: {
                        type: "string",
                        description: "Name of the door to close"
                    }
                }
            }
        }
    }];