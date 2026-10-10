import React, { Component } from 'react';
import FieldSet from 'Components/FieldSet';
import Link from 'Components/Link/Link';
import styles from '../styles.css';

const kofiUrl = 'https://ko-fi.com/timothyrice';

class Donations extends Component {

  //
  // Render

  render() {
    return (
      <FieldSet legend="Donations">
        <div className={styles.logoContainer} title="Support readarr-rresurrected on Ko-fi">
          <Link to={kofiUrl}>
            <img
              className={styles.logo}
              src={`${window.Readarr.urlBase}/Content/Images/Icons/logo-readarr.png`}
            />
          </Link>
        </div>
        <span className={styles.donationText}>
          If readarr-rresurrected is useful to you, you can support its development on <Link to={kofiUrl}>Ko-fi</Link>.
        </span>
      </FieldSet>
    );
  }
}

Donations.propTypes = {

};

export default Donations;
