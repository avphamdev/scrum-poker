import React from 'react';
import { Button } from '@scrpoker/components';
import style from './style.module.scss';
import { Actions } from '@scrpoker/store';
import { connect } from 'react-redux';
import { SUBMIT_POINT } from '@scrpoker/constants/apis';
import { getAuthHeader } from '@scrpoker/utils';

interface Props {
  className?: string;
  currentStory: IStory | undefined;
  roomCode: string;
  roomConnection: any;
  point: number;
  role: number;
  users: IUser[];
  submittedUsers: number;
  isLocked: boolean;
  roomState: string;
  updateIsLocked: (isLocked: boolean) => IRoomAction;
}

const ControlPanel: React.FC<Props> = ({
  users,
  submittedUsers,
  roomConnection,
  point,
  role,
  roomState,
  roomCode,
  isLocked,
  currentStory,
  updateIsLocked,
  className = '',
}) => {
  const currentStoryIsPicked = currentStory !== undefined ? true : false;
  const submitPoint = () => {
    const submitPointData = {
      storyId: currentStory?.id,
      point: point,
      isFinalPoint: false,
    };
    fetch(SUBMIT_POINT, {
      method: 'POST',
      body: JSON.stringify(submitPointData),
      headers: {
        'Content-Type': 'application/json',
        Authorization: getAuthHeader(),
      },
    });
  };
  return (
    <div className={`${style.controlPanel} ${className}`}>
      {role === 0 ? (
        roomState === 'waiting' ? (
          <Button
            className={style.button}
            onClick={() => {
              roomConnection.send('ChangeRoomState', roomCode, 'playing');
            }}
            disabled={!currentStoryIsPicked}
          >
            Start
          </Button>
        ) : roomState === 'playing' ? (
          <React.Fragment>
            <Button
              className={style.button}
              disabled={point === -1 || isLocked ? true : false}
              onClick={() => {
                roomConnection.send('ChangeUserStatus', roomCode, 'ready', point);
                updateIsLocked(true);
                submitPoint();
              }}
            >
              Lock
            </Button>
            <Button
              className={style.button}
              disabled={users.length !== submittedUsers}
              onClick={() => {
                roomConnection.send('ChangeRoomState', roomCode, 'revealed');
              }}
            >
              Reveal
            </Button>
          </React.Fragment>
        ) : (
          <Button
            className={style.button}
            onClick={() => {
              roomConnection.send('ChangeRoomState', roomCode, 'waiting');
              roomConnection.send('ChangeCurrentStory', roomCode, -1);
            }}
            disabled={false}
          >
            Done
          </Button>
        )
      ) : roomState === 'waiting' || roomState === 'revealed' ? (
        <React.Fragment />
      ) : (
        <Button
          className={style.button}
          disabled={point === -1 || isLocked ? true : false}
          onClick={() => {
            roomConnection.send('ChangeUserStatus', roomCode, 'ready', point);
            updateIsLocked(true);
            submitPoint();
          }}
        >
          Lock
        </Button>
      )}
    </div>
  );
};

const mapStateToProps = ({
  roomData: { roomCode, roomState, roomConnection, point, isLocked, currentStory, users, submittedUsers, role },
}: IGlobalState) => {
  return {
    roomCode,
    roomState,
    roomConnection,
    point,
    isLocked,
    currentStory,
    role,
    users,
    submittedUsers,
  };
};

const mapDispatchToProps = {
  updateIsLocked: Actions.roomActions.updateIsLocked,
};

export default connect(mapStateToProps, mapDispatchToProps)(ControlPanel);
