import React from 'react';
import './App.css';

import { Layout } from './components/Layout';
import { Route } from 'react-router-dom';
import { Home } from './components/Home';
import { FetchData } from './components/FetchData';
import { Counter } from './components/Counter';

const App: React.FC = () => {
  return (
    <Layout>
      <Route exact path='/' component={Home} />
      <Route path='/counter' component={Counter} />
      <Route path='/fetch-data' component={FetchData} />
    </Layout>
  );
};

export default App;
