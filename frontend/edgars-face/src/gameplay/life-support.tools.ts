import type {OllamaFunctionDefinition, OllamaToolDefinition} from "../rest.api.ts";

export  interface LifeSupportSystem {
    id: string,
    value: number,
    unit: string,
    allowed_units: string[]
}

export const lifeSupportTools: OllamaToolDefinition[] = [
    {
        type: "function",
        function: {
            name: "get_life_support_status",
            description: "Retrieves current status of life support systems on the ship.",
        } as OllamaFunctionDefinition
    },
    {
        type: "function",
        function: {
            name: "set_life_support_system",
            description: "Sets the value of a life support system on the ship.",
            parameters: {
                type: "object",
                properties: {
                    system_id: {
                        type: "string",
                        description: "ID of the life support system to set"
                    },
                    value: {
                        type: "string",
                        description: "Value to set the system to"
                    },
                    unit: {
                        type: "string",
                        description: "Unit of the value"
                    }
                }
            }
        } as OllamaFunctionDefinition
    },
]
