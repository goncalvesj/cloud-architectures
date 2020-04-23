import React, { Component } from 'react';

interface ICounterProps {}
type CounterState = {
  count: number;
};

export class Counter extends Component<ICounterProps, CounterState> {
  state: CounterState = {
    count: 0,
  };
  incrementCounter = () => {
    this.setState((state) => ({
      count: state.count + 1,
    }));
  };
  render() {
    return (
      <div>
        <h1>Counter</h1>
        <p>This is a simple example of a React component.</p>
        <p aria-live='polite'>
          Current count: <strong>{this.state.count}</strong>
        </p>
        <button
          className='btn btn-primary'
          onClick={() => this.incrementCounter()}
        >
          Increment
        </button>
      </div>
    );
  }
}
