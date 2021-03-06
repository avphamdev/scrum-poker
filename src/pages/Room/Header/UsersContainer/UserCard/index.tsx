import { Avatar } from '@scrpoker/components';
import Card from './Card';
import { Icon, Typo } from '@scrpoker/components';
import React from 'react';
import style from './style.module.scss';

interface Props {
  name: string;
  status: string;
  point?: number;
  role: number;
  className?: string;
}

const UserCard: React.FC<Props> = ({ name, status, point, role, className = '' }) => {
  return (
    <div className={`${style.userCard} ${className}`}>
      {role === 0 ? <Icon className={style.host} name="crown" size="lg" /> : ''}
      <Card status={status} point={point} />
      <Avatar className={style.avatar} letter={name[0].toUpperCase()} />
      <div className={style.userNameContainer}>
        <Typo className={style.userName}>{name}</Typo>
      </div>
      
    </div>
  );
};

export default UserCard;
