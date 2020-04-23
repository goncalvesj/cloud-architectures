import React, { Component } from 'react';

interface IProps {}
type State = {
  forecasts: [];
  loading: boolean;
  id: number;
};

export class FetchData extends Component<IProps, State> {
  state: State = {
    forecasts: [],
    loading: true,
    id: 0,
  };
  componentDidMount = async () => {
    this.populateWeatherData();
    this.getIds();
  };
  populateWeatherData = async () => {
    const response = await fetch('weatherforecast');
    const data = await response.json();
    // this.setState({ forecasts: data, loading: false });
    this.setState(() => ({
      forecasts: data,
      loading: false,
    }));
  };

  getIds = async () => {
    const requestOptions = {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ title: 'React POST Request Example' }),
    };
    const response = await fetch('weatherforecast', requestOptions);
    const data = await response.json();

    this.setState(() => ({
      id: data,
    }));
  };

  render() {
    return <div>Fetch, ID={this.state.id}</div>;
  }
}
