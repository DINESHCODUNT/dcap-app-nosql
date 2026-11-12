import React from 'react';
import Header from './Header';
import Footer from './Footer';
import HeaderMenu from './HeaderMenu';
import { Outlet } from 'react-router-dom';

const AppLayout: React.FC = () => {
  return (
    <>
      <div className="fixed-top">
        <Header />
        <HeaderMenu />
      </div>
      <main className='content-area'>
        <Outlet />
      </main>
      <Footer />
    </>
  );
};

export default AppLayout;