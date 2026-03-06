import type {OllamaFunctionDefinition, OllamaToolDefinition} from "../rest.api.ts";

export declare type DoorStatus = 'locked' | 'open' | 'closed' | 'jammed'

export const doorsToolDefinitions: OllamaToolDefinition[] = [{
    type: "function",
    function: {
        name: "list_doors",
        description: "Get all known doors on the ship and their status",
    } as OllamaFunctionDefinition
},
    {
        type: "function",
        function: {
            name: "set_door_state",
            description: "Set doors to one of the following states: locked, open, closed. Jammed doors cannot be changed.",
            parameters: {
                type: "object",
                properties: {
                    id: {
                        type: "string",
                        enum: ["locked", "open", "closed", "jammed"],
                        description: "ID of the door to change state"
                    },
                    state: {
                        type: "string",
                        enum: ["locked", "open", "closed", "jammed"],
                        description: "Status to set the door to"
                    }
                },
                required: ["id", "state"]
            }
        }
    }
];