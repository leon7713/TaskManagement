import React from 'react';
import { Task } from '../types/task.types';
import './TaskList.css';

interface TaskListProps {
  tasks: Task[];
  onEdit: (task: Task) => void;
  onDelete: (id: number) => void;
  loading: boolean;
}

const TaskList: React.FC<TaskListProps> = ({ tasks, onEdit, onDelete, loading }) => {
  const formatDate = (date: string) => {
    return new Date(date).toLocaleString();
  };

  const getPriorityLabel = (priority: number) => {
    const labels = ['', 'Low', 'Medium Low', 'Medium', 'Medium High', 'High'];
    return labels[priority] || 'Unknown';
  };

  const getPriorityClass = (priority: number) => {
    if (priority >= 4) return 'priority-high';
    if (priority === 3) return 'priority-medium';
    return 'priority-low';
  };

  if (loading) {
    return <div className="loading">Loading tasks...</div>;
  }

  if (tasks.length === 0) {
    return <div className="no-tasks">No tasks found. Create your first task!</div>;
  }

  return (
    <div className="task-list">
      {tasks.map((task) => (
        <div key={task.id} className={`task-card ${task.isCompleted ? 'completed' : ''} ${task.isOverdue ? 'overdue' : ''}`}>
          <div className="task-header">
            <h3>{task.title}</h3>
            <span className={`priority-badge ${getPriorityClass(task.priority)}`}>
              {getPriorityLabel(task.priority)}
            </span>
          </div>

          <p className="task-description">{task.description}</p>

          <div className="task-details">
            <div className="detail-item">
              <strong>Due Date:</strong> {formatDate(task.dueDate)}
            </div>
            <div className="detail-item">
              <strong>Assigned To:</strong> {task.fullName}
            </div>
            <div className="detail-item">
              <strong>Email:</strong> {task.email}
            </div>
            <div className="detail-item">
              <strong>Phone:</strong> {task.telephone}
            </div>
          </div>

          <div className="task-status">
            {task.isCompleted && <span className="status-badge completed">✓ Completed</span>}
            {task.isOverdue && !task.isCompleted && <span className="status-badge overdue">⚠ Overdue</span>}
          </div>

          <div className="task-actions">
            <button className="btn-edit" onClick={() => onEdit(task)}>
              Edit
            </button>
            <button className="btn-delete" onClick={() => onDelete(task.id)}>
              Delete
            </button>
          </div>
        </div>
      ))}
    </div>
  );
};

export default TaskList;
