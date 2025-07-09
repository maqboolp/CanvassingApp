export interface PasswordValidationResult {
  isValid: boolean;
  errors: string[];
}

export const validatePassword = (password: string): PasswordValidationResult => {
  const errors: string[] = [];
  
  if (password.length < 6) {
    errors.push('Password must be at least 6 characters');
  }
  
  if (!/\d/.test(password)) {
    errors.push('Password must contain at least one digit');
  }
  
  if (!/[a-z]/.test(password)) {
    errors.push('Password must contain at least one lowercase letter');
  }
  
  if (!/[A-Z]/.test(password)) {
    errors.push('Password must contain at least one uppercase letter');
  }
  
  return {
    isValid: errors.length === 0,
    errors
  };
};

export const getPasswordHelperText = (password: string): string => {
  if (!password) {
    return 'Minimum 6 characters, must include uppercase, lowercase, and a digit';
  }
  
  const validation = validatePassword(password);
  
  if (validation.isValid) {
    return 'Password meets all requirements ✓';
  }
  
  // Return the first error
  return validation.errors[0] || 'Invalid password';
};

export const PASSWORD_REQUIREMENTS = 'Minimum 6 characters, must include uppercase, lowercase, and a digit';