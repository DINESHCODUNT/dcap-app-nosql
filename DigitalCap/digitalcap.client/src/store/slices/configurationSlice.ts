import { createSlice, createAsyncThunk } from "@reduxjs/toolkit";
import type { PayloadAction } from "@reduxjs/toolkit";
import { getConfiguration } from "../../service/ConfigurationService";
import type { AppConfiguration } from "../../Interface/AppConfiguration";

interface ConfigurationState {
    data: AppConfiguration | null;
    loading: boolean;
    error: string | null;
}

const initialState: ConfigurationState = {
    data: null,
    loading: false,
    error: null,
};

export const fetchAppConfiguration = createAsyncThunk(
    "configuration/fetchAppConfiguration",
    async (_, { rejectWithValue }) => {
        try {
            const response = await getConfiguration();
            return response.data;
        } catch (error: any) {
            return rejectWithValue(
                error?.response?.data?.message ?? "Failed to load configuration"
            );
        }
    }
);

const configurationSlice = createSlice({
    name: "configuration",
    initialState,
    reducers: {
        clearConfiguration: (state) => {
            state.data = null;
            state.error = null;
        },
    },
    extraReducers: (builder) => {
        builder
            .addCase(fetchAppConfiguration.pending, (state) => {
                state.loading = true;
                state.error = null;
            })
            .addCase(
                fetchAppConfiguration.fulfilled,
                (state, action: PayloadAction<AppConfiguration>) => {
                    state.loading = false;
                    state.data = action.payload;
                }
            )
            .addCase(fetchAppConfiguration.rejected, (state, action) => {
                state.loading = false;
                state.error = action.payload as string;
            });
    },
});

export const { clearConfiguration } = configurationSlice.actions;
export default configurationSlice.reducer;