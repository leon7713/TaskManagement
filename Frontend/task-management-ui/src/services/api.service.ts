import axios from 'axios';
import { Task, CreateTaskDto, UpdateTaskDto } from '../types/task.types';

// Allow overriding the API base URL via environment variable to avoid hardcoded
// ports/protocol mismatches across dev setups.
const API_BASE_URL = process.env.REACT_APP_API_BASE_URL || 'http://localhost:7123/api';

const api = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

export const taskApi = {
  getAllTasks: async (): Promise<Task[]> => {
    const response = await api.get<Task[]>('/tasks');
    return response.data;
  },

  getTaskById: async (id: number): Promise<Task> => {
    const response = await api.get<Task>(`/tasks/${id}`);
    return response.data;
  },

  createTask: async (task: CreateTaskDto): Promise<Task> => {
    const response = await api.post<Task>('/tasks', task);
    return response.data;
  },

  updateTask: async (id: number, task: UpdateTaskDto): Promise<void> => {
    await api.put(`/tasks/${id}`, task);
  },

  deleteTask: async (id: number): Promise<void> => {
    await api.delete(`/tasks/${id}`);
  },

  getOverdueTasks: async (): Promise<Task[]> => {
    const response = await api.get<Task[]>('/tasks/overdue');
    return response.data;
  },
};
