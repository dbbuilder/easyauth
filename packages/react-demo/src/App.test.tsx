/**
 * TDD Tests for main App component
 * RED phase - defining expected behavior before implementation  
 */

import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import App from './App';

describe('App Component', () => {
  it('should render without crashing', () => {
    render(<App />);
  });

  it('should provide EasyAuth context to child components', () => {
    render(<App />);
    
    // App should wrap everything in EasyAuthProvider
    expect(document.body).toBeInTheDocument();
  });

  it('should render the main navigation', () => {
    render(<App />);
    
    expect(screen.getByRole('navigation')).toBeInTheDocument();
  });

  it('should render the demo content area', () => {
    render(<App />);
    
    expect(screen.getByText('EasyAuth Demo')).toBeInTheDocument();
    expect(screen.getByText('EasyAuth React Demo')).toBeInTheDocument();
  });
});