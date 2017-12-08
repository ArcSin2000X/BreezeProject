import { Component, OnInit, OnDestroy } from '@angular/core';
import { NgbModal, NgbActiveModal, NgbDropdown } from '@ng-bootstrap/ng-bootstrap';
import { FormGroup, FormControl, Validators, FormBuilder } from '@angular/forms';

import { PasswordConfirmationComponent } from './password-confirmation/password-confirmation.component';
import { ApiService } from '../../shared/services/api.service';
import { GlobalService } from '../../shared/services/global.service';
import { WalletInfo } from '../../shared/classes/wallet-info';
import { TumblebitService } from './tumblebit.service';
import { TumblerConnectionRequest } from './classes/tumbler-connection-request';
import { TumbleRequest } from './classes/tumble-request';
import { CycleInfo } from './classes/cycle-info';
import { ModalService } from '../../shared/services/modal.service';

import { Observable } from 'rxjs/Rx';
import { Subscription } from 'rxjs/Subscription';

@Component({
  selector: 'tumblebit-component',
  providers: [TumblebitService],
  templateUrl: './tumblebit.component.html',
  styleUrls: ['./tumblebit.component.css'],
})

export class TumblebitComponent implements OnInit {
  constructor(private apiService: ApiService, private tumblebitService: TumblebitService, private globalService: GlobalService, private modalService: NgbModal, private genericModalService: ModalService, private fb: FormBuilder) {
    this.buildTumbleForm();
  }
  public confirmedBalance: number;
  public unconfirmedBalance: number;
  public totalBalance: number;
  private walletBalanceSubscription: Subscription;
  public destinationWalletName: string;
  public destinationConfirmedBalance: number;
  public destinationUnconfirmedBalance: number;
  public destinationTotalBalance: number;
  public destinationWalletBalanceSubscription: Subscription;
  public isConnected: Boolean = false;
  public isSynced: Boolean = false;
  private walletStatusSubscription: Subscription;
  public tumblerAddressCopied: boolean = false;
  public tumblerParameters: any;
  public estimate: number;
  public fee: number;
  public denomination: number;
  private tumbleStatus: any;
  private tumbleStateSubscription: Subscription;
  private progressSubscription: Subscription;
  public progressDataArray: CycleInfo[];
  public tumbleForm: FormGroup;
  public tumbling: Boolean = false;
  private connectForm: FormGroup;
  public wallets: [string];
  public tumblerAddress: string = "Connecting...";
  public hasRegistrations: Boolean = false;

  ngOnInit() {
    this.checkWalletStatus();
    this.checkTumblingStatus();
    this.getWalletFiles();
    this.getWalletBalance();
  };

  ngOnDestroy() {
    if (this.walletBalanceSubscription){
      this.walletBalanceSubscription.unsubscribe();
    }

    if (this.destinationWalletBalanceSubscription) {
      this.destinationWalletBalanceSubscription.unsubscribe();
    }

    if (this.tumbleStateSubscription) {
      this.tumbleStateSubscription.unsubscribe();
    }

    if (this.walletStatusSubscription) {
      this.walletStatusSubscription.unsubscribe();
    }

    if (this.progressSubscription) {
      this.progressSubscription.unsubscribe();
    }
  };

  private buildTumbleForm(): void {
    this.tumbleForm = this.fb.group({
      'selectWallet': ['', Validators.required]
    });

    this.tumbleForm.valueChanges
      .subscribe(data => this.onValueChanged(this.tumbleForm, this.tumbleFormErrors, data));

    this.onValueChanged(this.tumbleForm, this.tumbleFormErrors);
  }

  // TODO: abstract to a shared utility lib
  onValueChanged(originalForm: FormGroup, formErrors: object, data?: any) {
    this.destinationWalletName = this.tumbleForm.get("selectWallet").value;

    if (this.destinationWalletName) {
      this.getDestinationWalletBalance();
    }

    if (!originalForm) { return; }
    const form = originalForm;
    for (const field in formErrors) {
      formErrors[field] = '';
      const control = form.get(field);
      if (control && control.dirty && !control.valid) {
        const messages = this.validationMessages[field];
        for (const key in control.errors) {
          formErrors[field] += messages[key] + ' ';
        }
      }
    }
  }
  tumbleFormErrors = {
    'selectWallet': ''
  }

  validationMessages = {
    'selectWallet': {
      'required': 'A destination address is required.',
    }
  }

  private checkWalletStatus() {
    let walletInfo = new WalletInfo(this.globalService.getWalletName())
    this.walletStatusSubscription = this.apiService.getGeneralInfo(walletInfo)
      .subscribe(
        response =>  {
          if (response.status >= 200 && response.status < 400) {
            let generalWalletInfoResponse = response.json();
            if (generalWalletInfoResponse.lastBlockSyncedHeight = generalWalletInfoResponse.chainTip) {
              this.isSynced = true;
            } else {
              this.isSynced = false;
            }
          }
        },
        error => {
          console.log(error);
          if (error.status === 0) {
            this.genericModalService.openModal(null, null);
          } else if (error.status >= 400) {
            if (!error.json().errors[0]) {
              console.log(error);
            }
            else {
              this.genericModalService.openModal(null, error.json().errors[0].message);
            }
          }
        }
      )
    ;
  }

  private checkTumblingStatus() {
    this.tumbleStateSubscription = this.tumblebitService.getTumblingState()
      .subscribe(
        response => {
          if (response.status >= 200 && response.status < 400) {
            if (response.json().registrations >= response.json().minRegistrations) {
              this.hasRegistrations = true;
            } else {
              this.hasRegistrations = false;
            }

            if (!this.isConnected && this.hasRegistrations && this.isSynced) {
              this.connectToTumbler();
            }

            if (response.json().state === "OnlyMonitor") {
              this.tumbling = false;
            } else if (response.json().state === "Tumbling") {
              this.tumbling = true;
              if (!this.progressSubscription) {
                this.getProgress();
              }
              this.destinationWalletName = response.json().destinationWallet;
              this.getDestinationWalletBalance();
            }
          }
        },
        error => {
          console.error(error);
          if (error.status === 0) {
            this.genericModalService.openModal(null, 'Something went wrong while connecting to the TumbleBit Client. Please restart the application.');
          } else if (error.status >= 400) {
            if (!error.json().errors[0]) {
              console.error(error);
            }
            else {
              this.genericModalService.openModal(null, error.json().errors[0].message);
            }
          }
        }
      )
    ;
  }

  private connectToTumbler() {
    let connection = new TumblerConnectionRequest(
      this.tumblerAddress,
      this.globalService.getNetwork()
    );

    this.tumblebitService
      .connectToTumbler()
      .subscribe(
        // TODO abstract into shared utility method
        response => {
          if (response.status >= 200 && response.status < 400) {
            this.tumblerParameters = response.json();
            this.tumblerAddress = this.tumblerParameters.tumbler
            this.estimate = this.tumblerParameters.estimate / 3600;
            this.fee = this.tumblerParameters.fee * 100;
            this.denomination = this.tumblerParameters.denomination;
            this.isConnected = true;
          }
        },
        error => {
          console.error(error);
          this.isConnected = false;
          if (error.status === 0) {
            this.genericModalService.openModal(null, 'Something went wrong while connecting to the TumbleBit Client. Please restart the application.');
          } else if (error.status >= 400) {
            if (!error.json().errors[0]) {
              console.error(error);
            }
            else {
              this.genericModalService.openModal(null, error.json().errors[0].message);
            }
          }
        }
      )
    ;
  }

  private startTumbling() {
    if (!this.isConnected) {
      this.genericModalService.openModal(null, "Can't start tumbling when you're not connected to a server. Please try again later.");
    } else {
      this.getProgress();
      const modalRef = this.modalService.open(PasswordConfirmationComponent);
      modalRef.componentInstance.sourceWalletName = this.globalService.getWalletName();
      modalRef.componentInstance.destinationWalletName = this.destinationWalletName;
    }
  }

  private stopTumbling() {
    this.tumblebitService.stopTumbling()
      .subscribe(
        response => {
          if (response.status >= 200 && response.status < 400) {
            this.tumbling = false;
            this.progressSubscription.unsubscribe();
          }
        },
        error => {
          console.error(error);
          if (error.status === 0) {
            this.genericModalService.openModal(null, 'Something went wrong while connecting to the TumbleBit Client. Please restart the application.');
          } else if (error.status >= 400) {
            if (!error.json().errors[0]) {
              console.error(error);
            }
            else {
              this.genericModalService.openModal(null, error.json().errors[0].message);
            }
          }
        }
      )
    ;
  }

  private getProgress() {
    this.progressSubscription = this.tumblebitService.getProgress()
      .subscribe(
        response => {
          if (response.status >= 200 && response.status < 400) {
            let responseArray= JSON.parse(response.json()).CycleProgressInfoList;
            if (responseArray) {
              let responseData = responseArray;
              this.progressDataArray = [];
              for (let cycle of responseData) {
                let periodStart = cycle.Period.Start;
                let periodEnd = cycle.Period.End;
                let height = cycle.Height;
                let blocksLeft = cycle.BlocksLeft;
                let cycleStart = cycle.Start;
                let cycleFailed = cycle.Failed;
                let cycleAsciiArt = cycle.AsciiArt;
                let status = cycle.Status;
                let phase = this.getPhaseString(cycle.Phase);
                let phaseNumber = this.getPhaseNumber(cycle.Phase);

                this.progressDataArray.push(new CycleInfo(periodStart, periodEnd, height, blocksLeft, cycleStart, cycleFailed, cycleAsciiArt, status, phase, phaseNumber));
              }
            }
          }
        },
        error => {
          console.error(error);
          if (error.status === 0) {
            this.genericModalService.openModal(null, 'Something went wrong while connecting to the TumbleBit Client. Please restart the application.');
          } else if (error.status >= 400) {
            if (!error.json().errors[0]) {
              console.error(error);
            }
            else {
              this.genericModalService.openModal(null, error.json().errors[0].message);
            }
          }
        }
      )
    ;
  }

  private getPhaseNumber(phase: string) {
    switch (phase) {
      case "Registration":
        return 1;
      case "ClientChannelEstablishment":
        return 2;
      case "TumblerChannelEstablishment":
        return 3;
      case "PaymentPhase":
        return 4;
      case "TumblerCashoutPhase":
        return 5;
      case "ClientCashoutPhase":
        return 6;
    }
  }

  private getPhaseString(phase: string) {
    switch (phase) {
      case "Registration":
        return "Registration";
      case "ClientChannelEstablishment":
        return "Client Channel Establishment";
      case "TumblerChannelEstablishment":
        return "Tumbler Channel Establishment";
      case "PaymentPhase":
        return "Payment Phase";
      case "TumblerCashoutPhase":
        return "Tumbler Cashout Phase";
      case "ClientCashoutPhase":
        return "Client Cashout Phase";
    }
  }

  // TODO: move into a shared service
  private getWalletBalance() {
    let walletInfo = new WalletInfo(this.globalService.getWalletName())
    this.walletBalanceSubscription = this.apiService.getWalletBalance(walletInfo)
      .subscribe(
        response =>  {
          if (response.status >= 200 && response.status < 400) {
              let balanceResponse = response.json();
              this.confirmedBalance = balanceResponse.balances[0].amountConfirmed;
              this.unconfirmedBalance = balanceResponse.balances[0].amountUnconfirmed;
              this.totalBalance = this.confirmedBalance + this.unconfirmedBalance;
          }
        },
        error => {
          console.log(error);
          if (error.status === 0) {
            this.genericModalService.openModal(null, null);
          } else if (error.status >= 400) {
            if (!error.json().errors[0]) {
              console.log(error);
            }
            else {
              this.genericModalService.openModal(null, error.json().errors[0].message);
            }
          }
        }
      )
    ;
  };

  private getDestinationWalletBalance() {
    if (this.destinationWalletBalanceSubscription) {
      this.destinationWalletBalanceSubscription.unsubscribe();
    }
    this.destinationWalletBalanceSubscription = this.tumblebitService.getWalletDestinationBalance(this.destinationWalletName)
      .subscribe(
        response =>  {
          if (response.status >= 200 && response.status < 400) {
            let balanceResponse = response.json();
            this.destinationConfirmedBalance = balanceResponse.balances[0].amountConfirmed;
            this.destinationUnconfirmedBalance = balanceResponse.balances[0].amountUnconfirmed;
            this.destinationTotalBalance = this.destinationConfirmedBalance + this.destinationUnconfirmedBalance;
          }
        },
        error => {
          console.log(error);
          if (error.status === 0) {
            this.genericModalService.openModal(null, null);
          } else if (error.status >= 400) {
            if (!error.json().errors[0]) {
              console.log(error);
            }
            else {
              this.genericModalService.openModal(null, error.json().errors[0].message);
            }
          }
        }
      )
    ;
  };

  private getWalletFiles() {
    this.apiService.getWalletFiles()
      .subscribe(
        response => {
          if (response.status >= 200 && response.status < 400) {
            let responseMessage = response.json();
            this.wallets = responseMessage.walletsFiles;
            if (this.wallets.length > 0) {
              for (let wallet in this.wallets) {
                this.wallets[wallet] = this.wallets[wallet].slice(0, -12);
              }
              //this.updateWalletFileDisplay(this.wallets[0]);
            } else {
            }
          }
        },
        error => {
          if (error.status === 0) {
            this.genericModalService.openModal(null, null);
          } else if (error.status >= 400) {
            if (!error.json().errors[0]) {
              console.log(error);
            }
            else {
              this.genericModalService.openModal(null, error.json().errors[0].message);
            }
          }
        },
        () => {
          // this.destinationWalletName = this.tumbleForm.get("selectWallet").value;
          // this.getDestinationWalletBalance()
        }
      )
    ;
  }

  private updateWalletFileDisplay(walletName: string) {
    this.tumbleForm.patchValue({selectWallet: walletName})
  }
}
