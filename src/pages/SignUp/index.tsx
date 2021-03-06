import React, { useState } from 'react';
import { connect } from 'react-redux';
import { Link, useHistory } from 'react-router-dom';
import { Button, Typo, Input, Card } from '@scrpoker/components';
import style from './style.module.scss';
import { Actions } from '@scrpoker/store';

const USER_NAME = 'userName';
const PASSWORD = 'password';
const EMAIL = 'email';
const CONFIRM_PASSWORD = 'confirmPassword';

interface Props {
  signUp: (data: ISignUpData) => Promise<void | boolean>;
  setIsTokenValid: (isValid: boolean) => void;
}

const SignUp: React.FC<Props> = ({ signUp, setIsTokenValid }) => {
  const [userName, setUserName] = useState('');
  const [password, setPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [email, setEmail] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const history = useHistory();

  const goBack = () => history.goBack();

  const submit = async () => {
    if (confirmPassword !== password) {
      alert('The password confirmation does not match');
    } else if (password.includes(' ') || confirmPassword.includes(' ') || email.includes(' ')) {
      alert('Password and email cannot be empty or white space');
    } else if (password.length < 6) {
      alert('Password should be at least 6 characters');
    } else if (userName.length > 16) {
      alert(`Username characters can't exceed 20 characters`);
    } else if (userName.trim() != '' && password && confirmPassword && email) {
      setIsLoading(true);
      const signUpData: ISignUpData = {
        userName: userName.trim(),
        password: password,
        email: email,
      };

      try {
        const isSignUpSuccessful = await signUp(signUpData);
        setIsLoading(false);
        if (isSignUpSuccessful) {
          setIsTokenValid(true);
          history.push('/home');
        } else alert('Something went wrong');
      } catch (err) {
        console.log(err);
      }
    } else alert('Please fill up empty fields');
  };

  const handleTextChange = ({ target: { name, value } }: React.ChangeEvent<HTMLInputElement>) => {
    switch (name) {
      case USER_NAME:
        setUserName(value);
        break;
      case PASSWORD:
        setPassword(value);
        break;
      case CONFIRM_PASSWORD:
        setConfirmPassword(value);
        break;
      default:
        setEmail(value);
        break;
    }
  };

  return (
    <div className={style.container}>
      <Card width={450}>
        <div className={style.title}>
          <Typo type="h2">Sign Up</Typo>
          <Link to="/login">Sign In</Link>
        </div>
        <Input name={EMAIL} onTextChange={handleTextChange} placeholder="Enter your email" />
        <Input name={USER_NAME} onTextChange={handleTextChange} placeholder="Enter your username" />
        <Input name={PASSWORD} type="password" onTextChange={handleTextChange} placeholder="Enter your password" />
        <Input
          name={CONFIRM_PASSWORD}
          type="password"
          onTextChange={handleTextChange}
          placeholder="Confirm your password"
        />
        {isLoading ? (
          <Button fullWidth className={style.loadingButton} icon={'fas fa-circle-notch fa-spin'}></Button>
        ) : (
          <Button fullWidth onClick={submit}>
            Create
          </Button>
        )}

        <Button fullWidth secondary onClick={goBack}>
          Cancel
        </Button>
      </Card>
    </div>
  );
};

const mapDispatchToProps = {
  signUp: Actions.userActions.signUp,
};

export default connect(null, mapDispatchToProps)(SignUp);
