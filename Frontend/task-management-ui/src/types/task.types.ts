export interface Task {
  id: number;
  title: string;
  description: string;
  dueDate: string;
  priority: number;
  fullName: string;
  telephone: string;
  email: string;
  createdAt: string;
  updatedAt?: string;
  isOverdue: boolean;
  isCompleted: boolean;
}

export interface CreateTaskDto {
  title: string;
  description: string;
  dueDate: string;
  priority: number;
  fullName: string;
  telephone: string;
  email: string;
}

export interface UpdateTaskDto extends CreateTaskDto {
  isCompleted: boolean;
}

export interface TaskState {
  tasks: Task[];
  loading: boolean;
  error: string | null;
  selectedTask: Task | null;
}
