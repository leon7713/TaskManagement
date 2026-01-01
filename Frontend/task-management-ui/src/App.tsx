import React, { useEffect, useState } from 'react';
import { useAppDispatch, useAppSelector } from './store/hooks';
import { fetchTasks, createTask, updateTask, deleteTask, setSelectedTask, clearError } from './store/taskSlice';
import TaskList from './components/TaskList';
import TaskForm from './components/TaskForm';
import { CreateTaskDto, UpdateTaskDto, Task } from './types/task.types';
import './App.css';

function App() {
  const dispatch = useAppDispatch();
  const { tasks, loading, error, selectedTask } = useAppSelector((state) => state.tasks);
  const [showForm, setShowForm] = useState(false);
  const [isEditMode, setIsEditMode] = useState(false);

  useEffect(() => {
    dispatch(fetchTasks());
  }, [dispatch]);

  useEffect(() => {
    if (error) {
      alert(`Error: ${error}`);
      dispatch(clearError());
    }
  }, [error, dispatch]);

  const handleCreateTask = () => {
    setShowForm(true);
    setIsEditMode(false);
    dispatch(setSelectedTask(null));
  };

  const handleEditTask = (task: Task) => {
    setShowForm(true);
    setIsEditMode(true);
    dispatch(setSelectedTask(task));
  };

  const handleDeleteTask = async (id: number) => {
    if (window.confirm('Are you sure you want to delete this task?')) {
      await dispatch(deleteTask(id));
      dispatch(fetchTasks());
    }
  };

  const handleSubmit = async (data: CreateTaskDto | UpdateTaskDto) => {
    try {
      if (isEditMode && selectedTask) {
        await dispatch(updateTask({ id: selectedTask.id, task: data as UpdateTaskDto }));
      } else {
        await dispatch(createTask(data as CreateTaskDto));
      }
      setShowForm(false);
      dispatch(setSelectedTask(null));
      dispatch(fetchTasks());
    } catch (err) {
      console.error('Error submitting task:', err);
    }
  };

  const handleCancel = () => {
    setShowForm(false);
    setIsEditMode(false);
    dispatch(setSelectedTask(null));
  };

  return (
    <div className="app">
      <header className="app-header">
        <h1>📋 Task Management System</h1>
      </header>

      <main className="app-main">
        {!showForm ? (
          <>
            <div className="toolbar">
              <button className="btn-create" onClick={handleCreateTask}>
                + Create New Task
              </button>
              <button className="btn-refresh" onClick={() => dispatch(fetchTasks())}>
                🔄 Refresh
              </button>
            </div>

            <TaskList
              tasks={tasks}
              onEdit={handleEditTask}
              onDelete={handleDeleteTask}
              loading={loading}
            />
          </>
        ) : (
          <TaskForm
            initialData={
              selectedTask
                ? {
                    title: selectedTask.title,
                    description: selectedTask.description,
                    dueDate: selectedTask.dueDate,
                    priority: selectedTask.priority,
                    fullName: selectedTask.fullName,
                    telephone: selectedTask.telephone,
                    email: selectedTask.email,
                    isCompleted: selectedTask.isCompleted,
                  }
                : undefined
            }
            onSubmit={handleSubmit}
            onCancel={handleCancel}
            isEdit={isEditMode}
          />
        )}
      </main>

      <footer className="app-footer">
        <p>© 2024 Task Management System</p>
      </footer>
    </div>
  );
}

export default App;
